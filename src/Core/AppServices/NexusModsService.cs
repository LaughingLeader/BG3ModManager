﻿using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Models;
using DivinityModManager.Models.NexusMods;
using DivinityModManager.Models.Updates;

using DynamicData;
using DynamicData.Binding;

using NexusModsNET;
using NexusModsNET.DataModels;

using Reactive.Bindings.Disposables;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

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

		private readonly CompositeDisposable _downloadTasksCompositeDisposable = new CompositeDisposable();

		private readonly NexusModsObservableApiLimits _apiLimits;
		public NexusModsObservableApiLimits ApiLimits => _apiLimits;

		protected ObservableCollectionExtended<string> _downloadResults = new ObservableCollectionExtended<string>();
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

		private async Task<System.IO.Stream> DownloadUrlAsStreamAsync(Uri downloadUrl, CancellationToken token)
		{
			try
			{
				using var webClient = new WebClient();
				webClient.Headers.Add("apikey", ApiKey);
				double receivedBytes = 0;

				var stream = await webClient.OpenReadTaskAsync(downloadUrl);
				var ms = new System.IO.MemoryStream();
				var buffer = new byte[4096];
				int read = 0;
				double totalBytes = double.Parse(webClient.ResponseHeaders[HttpResponseHeader.ContentLength]);

				while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
				{
					ms.Write(buffer, 0, read);
					receivedBytes += read;
					DownloadProgressValue = (receivedBytes / totalBytes) * 100d;
				}
				DownloadProgressValue = 100d;
				stream.Close();
				return ms;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error downloading url ({downloadUrl}):\n{ex}");
			}
			return null;
		}

		public async Task<bool> ProcessNXMLinkAsync(string url, IScheduler sch, CancellationToken token)
		{
			if (!CanFetchData) return false;

			try
			{
				var data = NexusModsProtocolData.FromUrl(url);
				if (data.IsValid)
				{
					var files = await _dataLoader.ModFiles.GetModFileDownloadLinksAsync(data.GameId, data.ModId, data.FileId, data.Key, data.Expires, token);
					if (files != null)
					{
						var file = files.FirstOrDefault();
						if (file != null)
						{
							var outputFolder = DivinityApp.GetAppDirectory("Downloads");
							Directory.CreateDirectory(outputFolder);
							var fileName = Path.GetFileName(WebUtility.UrlDecode(file.Uri.AbsolutePath));
							var filePath = Path.Combine(outputFolder, fileName);
							DivinityApp.Log($"Downloading {file.Uri} to {filePath}");
							DownloadProgressText = $"Downloading {fileName}...";
							DownloadProgressValue = 0;
							using (var stream = await DownloadUrlAsStreamAsync(file.Uri, token))
							{
								if(token.IsCancellationRequested)
								{
									DownloadProgressText = $"Stopped downloading {fileName}";
									DownloadProgressValue = 0;
									return false;
								}
								using (var outputStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
								{
									stream.Position = 0;
									await stream.CopyToAsync(outputStream, 4096, token);
									DownloadResults.Add(filePath);
									DivinityApp.Log("Download done.");
									DownloadProgressText = $"Downloaded {fileName}";
								}
							}
						}
					}
				}
				else
				{
					DivinityApp.Log($"nxm url ({url}) is not valid:\n{data}");
				}
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"Error processing nxm url ({url}):\n{ex}");
			}
			return false;
		}

		private IDisposable _scheduledClearTasks;

		public void ProcessNXMLinkBackground(string url)
		{
			var task = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch,token) => {
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

					if(ProfileAvatarUrl == null)
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