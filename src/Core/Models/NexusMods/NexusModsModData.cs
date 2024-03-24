using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using NexusModsNET.DataModels;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;

namespace ModManager.Models.NexusMods;

public class NexusModsModData : INotifyPropertyChanged
{
	[JsonProperty("uuid")]
	public string UUID { get; set; }

	private long _lastFileId;

	[JsonProperty("last_file_id")]
	public long LastFileId
	{
		get => _lastFileId;
		set
		{
			if (_lastFileId != value)
			{
				_lastFileId = value;
				RaisePropertyChanged(nameof(LastFileId));
			}
		}
	}

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("summary")]
	public string Summary { get; set; }

	//[JsonProperty("description")]
	//public string Description { get; set; }

	[JsonProperty("picture_url")]
	public Uri PictureUrl { get; set; }

	[JsonProperty("mod_id")]
	public long ModId { get; set; }

	[JsonProperty("category_id")]
	public long CategoryId { get; set; }

	[JsonProperty("version")]
	public string Version { get; set; }

	[JsonProperty("endorsement_count")]
	public long EndorsementCount { get; set; }

	[JsonProperty("created_timestamp")]
	public long CreatedTimestamp { get; set; }

	[JsonProperty("updated_timestamp")]
	public long UpdatedTimestamp { get; set; }

	[JsonProperty("author")]
	public string Author { get; set; }

	[JsonProperty("contains_adult_content")]
	public bool ContainsAdultContent { get; set; }

	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("available")]
	public bool Available { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void RaisePropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void SetModVersion(NexusModFileVersionData info)
	{
		if (info.Success)
		{
			SetModVersion(info.ModId, info.FileId);
		}
	}

	public void SetModVersion(long modId, long fileId = -1)
	{
		if (ModId != modId)
		{
			ModId = modId;
			RaisePropertyChanged(nameof(ModId));
		}

		if (fileId > LastFileId)
		{
			LastFileId = fileId;
		}
	}

	private static readonly IEnumerable<PropertyInfo> _lazySerializedProperties = typeof(NexusModsModData)
		.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		.Where(prop => prop.GetCustomAttribute<JsonPropertyAttribute>(true) != null);

	public void Update(NexusModsModData data)
	{
		foreach (var prop in _lazySerializedProperties)
		{
			var value = prop.GetValue(data);
			if (value != null)
			{
				prop.SetValue(this, value);
				RaisePropertyChanged(prop.Name);
			}
		}
		IsUpdated = true;
		RaisePropertyChanged(nameof(IsUpdated));
	}

	public void Update(NexusMod data)
	{
		var t = typeof(NexusMod);
		foreach (var prop in _lazySerializedProperties)
		{
			var nexusProp = t.GetProperty(prop.Name);
			if (nexusProp != null)
			{
				var value = nexusProp.GetValue(data);
				if (value != null)
				{
					prop.SetValue(this, value);
					RaisePropertyChanged(prop.Name);
				}
			}
		}
		IsUpdated = true;
		RaisePropertyChanged(nameof(IsUpdated));
	}

	[JsonIgnore] public bool IsUpdated { get; set; }

	/// <summary>
	/// True if ModId is set.
	/// </summary>
	[Reactive, JsonIgnore] public bool IsEnabled { get; private set; }

	public NexusModsModData()
	{
		ModId = -1;
		LastFileId = -1;

		this.WhenAnyValue(x => x.ModId)
		.Select(x => x >= DivinityApp.NEXUSMODS_MOD_ID_START)
		.ObserveOn(RxApp.MainThreadScheduler)
		.BindTo(this, x => x.IsEnabled);
	}
}
