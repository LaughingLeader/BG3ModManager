using DivinityModManager.AppServices.Data;
using DivinityModManager.Models.Mod;
using DivinityModManager.Models.NexusMods;
using DivinityModManager.Models.NexusMods.NXM;
using DivinityModManager.Models.Updates;

using DynamicData;
using DynamicData.Binding;

using Newtonsoft.Json;

using NexusModsNET;
using NexusModsNET.DataModels;
using NexusModsNET.DataModels.GraphQL.Query;

using Reactive.Bindings.Disposables;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;

namespace DivinityModManager
{
	public interface INexusModsService
	{
		string ApiKey { get; set; }
		bool IsInitialized { get; }
		bool LimitExceeded { get; }
		bool CanFetchData { get; }
		bool IsPremium { get; }
		Uri ProfileAvatarUrl { get; }

		double DownloadProgressValue { get; }
		string DownloadProgressText { get; }
		bool CanCancel { get; }

		NexusModsObservableApiLimits ApiLimits { get; }

		IObservable<NexusModsObservableApiLimits> WhenLimitsChange { get; }

		ObservableCollectionExtended<string> DownloadResults { get; }

		Task<Dictionary<string, NexusModsModDownloadLink>> GetLatestDownloadsForModsAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
		Task<UpdateResult> FetchModInfoAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
		void ProcessNXMLinkBackground(string url);
		void CancelDownloads();
	}
}

namespace DivinityModManager.AppServices
{
	public class NexusModsService : ReactiveObject, INexusModsService
	{
		private INexusModsClient _client;
		private InfosInquirer _dataLoader;

		[Reactive] public string ApiKey { get; set; }
		[Reactive] public bool IsPremium { get; private set; }
		[Reactive] public Uri ProfileAvatarUrl { get; private set; }
		[Reactive] public double DownloadProgressValue { get; private set; }
		[Reactive] public string DownloadProgressText { get; private set; }
		[Reactive] public bool CanCancel { get; private set; }

		private readonly CompositeDisposable _downloadTasksCompositeDisposable = new();

		private readonly NexusModsObservableApiLimits _apiLimits;
		public NexusModsObservableApiLimits ApiLimits => _apiLimits;

		protected ObservableCollectionExtended<string> _downloadResults = new();
		public ObservableCollectionExtended<string> DownloadResults => _downloadResults;

		public bool IsInitialized => _client != null;
		public bool LimitExceeded => LimitExceededCheck();
		public bool CanFetchData => IsInitialized && !LimitExceeded;

		private readonly IObservable<NexusModsObservableApiLimits> _whenLimitsChange;
		public IObservable<NexusModsObservableApiLimits> WhenLimitsChange => _whenLimitsChange;

		private bool LimitExceededCheck()
		{
			if (_client != null)
			{
				var daily = _client.RateLimitsManagement.ApiDailyLimitExceeded();
				var hourly = _client.RateLimitsManagement.ApiHourlyLimitExceeded();

				if (daily)
				{
					DivinityApp.Log($"Daily limit exceeded ({ApiLimits.DailyLimit})");
					return true;
				}
				else if (hourly)
				{
					DivinityApp.Log($"Hourly limit exceeded ({ApiLimits.HourlyLimit})");
					return true;
				}
			}
			return false;
		}

		public bool CanDoTask(int apiCalls)
		{
			if (_client != null)
			{
				var currentLimit = Math.Min(ApiLimits.HourlyRemaining, ApiLimits.DailyRemaining);
				if (currentLimit > apiCalls)
				{
					return true;
				}
			}
			return false;
		}

		public async Task<NexusUser> GetUserAsync(CancellationToken token)
		{
			if (!CanFetchData) return null;
			return await _dataLoader.User.GetUserAsync(token);
		}

		public async Task<Dictionary<string, NexusModsModDownloadLink>> GetLatestDownloadsForModsAsync(IEnumerable<DivinityModData> mods, CancellationToken token)
		{
			var links = new Dictionary<string, NexusModsModDownloadLink>();
			if (!CanFetchData) return links;

			try
			{
				//1 call for the mod files, 1 call to get a mod file link
				var apiCallAmount = mods.Count(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START) & 2;
				if (!CanDoTask(apiCallAmount))
				{
					DivinityApp.Log($"Task would exceed hourly or daily API limits. ExpectedCalls({apiCallAmount}) HourlyRemaining({ApiLimits.HourlyRemaining}/{ApiLimits.HourlyLimit}) DailyRemaining({ApiLimits.DailyRemaining}/{ApiLimits.DailyLimit})");
					return links;
				}
				foreach (var mod in mods)
				{
					if (mod.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START)
					{
						var result = await _dataLoader.ModFiles.GetModFilesAsync(DivinityApp.NEXUSMODS_GAME_DOMAIN, mod.NexusModsData.ModId, token);
						if (result != null)
						{
							var file = result.ModFiles.FirstOrDefault(x => x.IsPrimary || x.Category == NexusModFileCategory.Main);
							if (file != null && (mod.Version < file.ModVersion || mod.LastModified?.ToUnixTimeSeconds() < file.UploadedTimestamp))
							{
								var fileId = file.FileId;
								var linkResult = await _dataLoader.ModFiles.GetModFileDownloadLinksAsync(DivinityApp.NEXUSMODS_GAME_DOMAIN, mod.NexusModsData.ModId, fileId, token);
								if (linkResult != null && linkResult.Count() > 0)
								{
									var primaryLink = linkResult.FirstOrDefault();
									links.Add(mod.UUID, new NexusModsModDownloadLink(mod, primaryLink, file));
								}
							}
						}
					}

					if (token.IsCancellationRequested) break;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error fetching NexusMods data:\n{ex}");
			}

			return links;
		}

		public async Task<UpdateResult> FetchModInfoAsync(IEnumerable<DivinityModData> mods, CancellationToken token)
		{
			var taskResult = new UpdateResult();
			if (token.IsCancellationRequested)
			{
				taskResult.FailureMessage = "Task canceled.";
				return taskResult;
			}

			if (!CanFetchData)
			{
				if (_client == null)
				{
					taskResult.FailureMessage = "API Client not initialized.";
				}
				else
				{
					var rateLimits = ApiLimits;
					taskResult.FailureMessage = $"API limit exceeded. Hourly({rateLimits.HourlyRemaining}/{rateLimits.HourlyLimit}) Daily({rateLimits.DailyRemaining}/{rateLimits.DailyLimit})";
				}
				return taskResult;
			}
			var totalLoaded = 0;

			try
			{
				var targetMods = mods.Where(mod => mod.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START).ToList();
				var total = targetMods.Count;
				if (total == 0)
				{
					taskResult.Success = false;
					taskResult.FailureMessage = "Skipping. No mods to check (no NexusMods ID set in the loaded mods).";
					return taskResult;
				}

				var apiCallAmount = total; // 1 call for 1 mod
				if (!CanDoTask(total))
				{
					taskResult.Success = false;
					taskResult.FailureMessage = $"Task would exceed hourly or daily API limits. ExpectedCalls({apiCallAmount}) HourlyRemaining({ApiLimits.HourlyRemaining}/{ApiLimits.HourlyLimit}) DailyRemaining({ApiLimits.DailyRemaining}/{ApiLimits.DailyLimit})";
					return taskResult;
				}

				DivinityApp.Log($"Using NexusMods API to update {total} mods");

				foreach (var mod in targetMods)
				{
					if (token.IsCancellationRequested) break;

					var result = await _dataLoader.Mods.GetMod(DivinityApp.NEXUSMODS_GAME_DOMAIN, mod.NexusModsData.ModId, token);
					if (result != null)
					{
						mod.NexusModsData.Update(result);
						taskResult.UpdatedMods.Add(mod);
						totalLoaded++;
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error fetching NexusMods data:\n{ex}");
			}

			return taskResult;
		}

		private static async Task<Stream> DownloadUrlAsStreamAsync(string apiKey, Uri downloadUrl, CancellationToken token)
		{
			using var httpClient = new HttpClient();
			//TODO refactor into format Nexus expects, i.e. NexusApiClient/0.7.3 (Windows_NT 10.0.17134; x64) Node/8.9.3
			httpClient.DefaultRequestHeaders.Add("User-Agent", "BG3ModManager");
			httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
			try
			{
				var fileStream = await httpClient.GetStreamAsync(downloadUrl, token);
				return fileStream;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error downloading url ({downloadUrl}):\n{ex}");
				return Stream.Null;
			}
		}

		private static INexusModsProtocol GetProtocolData(string url)
		{
			if (url.Contains("collections"))
			{
				return NexusDownloadCollectionProtocolData.FromUrl(url);
			}
			return NexusDownloadModProtocolData.FromUrl(url);
		}

		public async Task<bool> ProcessNXMLinkAsync(string url, IScheduler sch, CancellationToken token)
		{
			if (!CanFetchData) return false;

			try
			{
				var data = GetProtocolData(url);
				if (!data.IsValid)
				{
					DivinityApp.Log($"nxm url ({url}) is not valid:\n{data}");
					return false;
				}
				if (data.GameDomain != DivinityApp.NEXUSMODS_GAME_DOMAIN)
				{
					DivinityApp.Log($"Game ({data.GameDomain}) is not Baldur's Gate 3 ({DivinityApp.NEXUSMODS_GAME_DOMAIN}). Skipping.");
					return false;
				}
				DownloadProgressValue = 0;
				while (!token.IsCancellationRequested)
				{
					switch (data.ProtocolType)
					{
						case NexusModsProtocolType.ModFile:
							var modProtocol = (NexusDownloadModProtocolData)data;
							var files = await _dataLoader.ModFiles.GetModFileDownloadLinksAsync(modProtocol.GameDomain,
								modProtocol.ModId, modProtocol.FileId, modProtocol.Key, modProtocol.Expires, token);
							if (files != null)
							{
								var file = files.FirstOrDefault();
								if (file != null)
								{
									var outputFolder = DivinityApp.GetAppDirectory("Downloads");
									Directory.CreateDirectory(outputFolder);
									var fileName = Path.GetFileName(WebUtility.UrlDecode(file.Uri.AbsolutePath));
									var filePath = Path.Join(outputFolder, fileName);
									DivinityApp.Log($"Downloading {file.Uri} to {filePath}");
									DownloadProgressText = $"Downloading {fileName}...";
									DownloadProgressValue = 0;
									using var stream = await DownloadUrlAsStreamAsync(ApiKey, file.Uri, token);
									using var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 128000, true);
									await stream.CopyToAsync(outputStream, 128000, token);
									DownloadResults.Add(filePath);
									DivinityApp.Log("Download done.");
									DownloadProgressText = $"Downloaded {fileName}";
									return true;
								}
							}
							break;

						case NexusModsProtocolType.Collection:
							var collectionProtocol = (NexusDownloadCollectionProtocolData)data;
							var allowAdultContent = Services.Settings.ManagerSettings.UpdateSettings.AllowAdultContent;

							var queryData = new NexusGraphQueryCollectionRevisionRequestData(collectionProtocol.GameDomain, collectionProtocol.Slug,
								collectionProtocol.Revision, allowAdultContent, NexusModsQuery.CollectionRevision);
							var payload = JsonConvert.SerializeObject(queryData);
							var content = new StringContent(payload, Encoding.UTF8, "application/json");

							var collectionData = await _dataLoader.Graph.PostAsync<NexusGraphQueryCollectionRevisionResult>(content, token);
							if (collectionData?.Data != null)
							{
								var modFiles = collectionData.Data.CollectionRevision?.ModFiles;
								if (modFiles != null && modFiles.Length > 0)
								{
									DivinityApp.Log($"Total mods in collection: {modFiles.Length}");
									var doDownload = await DivinityInteractions.OpenDownloadCollectionView.Handle(collectionData.Data.CollectionRevision);
									DivinityApp.Log($"doDownload: {doDownload}");
									if (doDownload)
									{

									}
								}
							}
							break;
					}
					return false;
				}
				DownloadProgressText = $"Stopped downloading mod file.";
				return false;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error processing nxm url ({url}):\n{ex}");
			}
			return false;
		}

		private IDisposable _scheduledClearTasks;

		public void ProcessNXMLinkBackground(string url)
		{
			var task = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, token) =>
			{
				_scheduledClearTasks?.Dispose();
				await ProcessNXMLinkAsync(url, sch, token);
				_scheduledClearTasks = sch.Schedule(TimeSpan.FromMilliseconds(250), ClearTasks);
			});
			_downloadTasksCompositeDisposable.Add(task);
			CanCancel = true;
		}

		private void ClearTasks()
		{
			_downloadTasksCompositeDisposable.Clear();
			_scheduledClearTasks?.Dispose();
			CanCancel = false;
		}

		public void CancelDownloads()
		{
			ClearTasks();
		}

		public NexusModsService(string appName, string appVersion)
		{
			_apiLimits = new NexusModsObservableApiLimits();
			_whenLimitsChange = _apiLimits.WhenAnyPropertyChanged();
			this.WhenAnyValue(x => x.ApiKey).Subscribe(key =>
			{
				_client?.Dispose();
				_dataLoader?.Dispose();

				if (!String.IsNullOrEmpty(key))
				{
					_client = NexusModsClient.Create(key, appName, appVersion, _apiLimits);
					_dataLoader = new InfosInquirer(_client);

					if (ProfileAvatarUrl == null)
					{
						DivinityApp.Log("Fetching NexusMods user profile info...");
						RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, cts) =>
						{
							var user = await GetUserAsync(cts);
							if (user != null)
							{
								RxApp.MainThreadScheduler.Schedule(() =>
								{
									IsPremium = user.IsPremium;
									ProfileAvatarUrl = user.ProfileAvatarUrl;
								});
							}
							else
							{
								DivinityApp.Log("Failed to fetch NexusMods user profile info.");
							}
						});
					}
				}
				else
				{
					_apiLimits.Reset();
				}
			});
		}
	}
}