﻿namespace DivinityModManager.Models;

public class DivinityModWorkshopData : ReactiveObject
{
	private string id = "";

	public string ID
	{
		get => id;
		set { this.RaiseAndSetIfChanged(ref id, value); }
	}

	private DateTime createdDate;

	public DateTime CreatedDate
	{
		get => createdDate;
		set { this.RaiseAndSetIfChanged(ref createdDate, value); }
	}

	private DateTime updatedDate;

	public DateTime UpdatedDate
	{
		get => updatedDate;
		set { this.RaiseAndSetIfChanged(ref updatedDate, value); }
	}

	private List<string> tags;

	public List<string> Tags
	{
		get => tags;
		set
		{
			this.RaiseAndSetIfChanged(ref tags, value);
		}
	}
}
