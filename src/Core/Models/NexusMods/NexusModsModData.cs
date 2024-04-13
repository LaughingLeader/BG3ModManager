using NexusModsNET.DataModels;

using System.Reflection;
using System.Runtime.Serialization;

namespace ModManager.Models.NexusMods;

public class NexusModsModData : ReactiveObject
{
	[JsonPropertyName("uuid")]
	public string? UUID { get; set; }

	[JsonPropertyName("last_file_id"), Reactive]
	public long LastFileId { get; set; }

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("summary")]
	public string? Summary { get; set; }

	//[JsonPropertyName("description")]
	//public string? Description { get; set; }

	[JsonPropertyName("picture_url")]
	public Uri? PictureUrl { get; set; }

	[JsonPropertyName("mod_id"), Reactive]
	public long ModId { get; set; }

	[JsonPropertyName("category_id")]
	public long CategoryId { get; set; }

	[JsonPropertyName("version")]
	public string? Version { get; set; }

	[JsonPropertyName("endorsement_count")]
	public long EndorsementCount { get; set; }

	[JsonPropertyName("created_timestamp")]
	public long CreatedTimestamp { get; set; }

	[JsonPropertyName("updated_timestamp")]
	public long UpdatedTimestamp { get; set; }

	[JsonPropertyName("author")]
	public string? Author { get; set; }

	[JsonPropertyName("contains_adult_content")]
	public bool ContainsAdultContent { get; set; }

	[JsonPropertyName("status")]
	public string? Status { get; set; }

	[JsonPropertyName("available")]
	public bool Available { get; set; }

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
		}

		if (fileId > LastFileId)
		{
			LastFileId = fileId;
		}
	}

	private static readonly IEnumerable<PropertyInfo> _lazySerializedProperties = typeof(NexusModsModData)
		.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		.Where(prop => prop.GetCustomAttribute<DataMemberAttribute>(true) != null);

	public void Update(NexusModsModData data)
	{
		foreach (var prop in _lazySerializedProperties)
		{
			var value = prop.GetValue(data);
			if (value != null)
			{
				prop.SetValue(this, value);
				this.RaisePropertyChanged(prop.Name);
			}
		}
		IsUpdated = true;
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
					this.RaisePropertyChanged(prop.Name);
				}
			}
		}
		IsUpdated = true;
	}

	[Reactive, JsonIgnore] public bool IsUpdated { get; set; }

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
