using ModManager.Models.Mod;

using System.Globalization;

namespace ModManager.Models;

public struct DivinityModFilterData
{
	public string FilterProperty { get; set; }
	public string FilterValue { get; set; }

	private static readonly char[] separators = new char[1] { ' ' };

	public bool ValueContains(string val, bool separateWhitespace = false)
	{
		if (separateWhitespace && val.IndexOf(" ") > 1)
		{
			var vals = val.Split(separators, StringSplitOptions.RemoveEmptyEntries);
			var findVals = FilterValue.Split(separators, StringSplitOptions.RemoveEmptyEntries);
			DivinityApp.Log($"Searching for '{String.Join("; ", findVals)}' in ({String.Join("; ", vals)}");
			return vals.Any(x => findVals.Any(x2 => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x, x2, CompareOptions.IgnoreCase) >= 0));
		}
		else
		{
			return CultureInfo.CurrentCulture.CompareInfo.IndexOf(val, FilterValue, CompareOptions.IgnoreCase) >= 0;
		}
	}

	public bool PropertyContains(string val)
	{
		return CultureInfo.CurrentCulture.CompareInfo.IndexOf(FilterProperty, val, CompareOptions.IgnoreCase) >= 0;
	}

	public bool Match(IModEntry entry)
	{
		if (String.IsNullOrWhiteSpace(FilterValue)) return true;

		if (PropertyContains("Author") && entry.Author.IsValid())
		{
			if (ValueContains(entry.Author)) return true;
		}

		if (PropertyContains("Version") && entry.Version.IsValid())
		{
			if (ValueContains(entry.Version)) return true;
		}

		if (PropertyContains("Name") && entry.DisplayName.IsValid())
		{
			//DivinityApp.LogMessage($"Searching for '{FilterValue}' in '{mod.Name}' | {mod.Name.IndexOf(FilterValue)}");
			if (ValueContains(entry.DisplayName)) return true;
		}

		if (PropertyContains("UUID") && entry.UUID.IsValid())
		{
			if (ValueContains(entry.UUID)) return true;
		}

		if (PropertyContains("Selected") && entry.IsSelected)
		{
			return true;
		}

		if (entry is ModEntry mentry && mentry.Data != null)
		{
			var mod = mentry.Data;

			if (PropertyContains("Mode"))
			{
				foreach (var mode in mod.Modes)
				{
					if (ValueContains(mode))
					{
						return true;
					}
				}
			}

			if (PropertyContains("Depend"))
			{
				foreach (var dependency in mod.Dependencies.Items)
				{
					if (ValueContains(dependency.Name) || FilterValue == dependency.UUID || ValueContains(dependency.Folder))
					{
						return true;
					}
				}
			}

			if (PropertyContains("File"))
			{
				if (ValueContains(mod.FileName)) return true;
			}

			if (PropertyContains("Desc"))
			{
				if (ValueContains(mod.Description)) return true;
			}

			if (PropertyContains("Type"))
			{
				if (ValueContains(mod.ModType)) return true;
			}

			if (PropertyContains("Editor"))
			{
				if (mod.IsEditorMod) return true;
			}

			if (PropertyContains("Modified") || PropertyContains("Updated"))
			{
				var date = DateTimeOffset.Now;
				if (DateTimeOffset.TryParse(FilterValue, out date))
				{
					if (mod.LastModified >= date) return true;
				}
			}

			if (PropertyContains("Tag"))
			{
				if (mod.Tags != null && mod.Tags.Count > 0)
				{
					var f = this;
					if (mod.Tags.Any(x => f.ValueContains(x))) return true;
					// GM, Story, Arena are technically tags as well
					foreach (var mode in mod.Modes)
					{
						if (ValueContains(mode))
						{
							return true;
						}
					}
				}
			}
		}

		/*
		 *	var propertyValue = (string)mod.GetType().GetProperty(FilterProperty).GetValue(mod, null);
			if(propertyValue != null)
			{
				return CultureInfo.CurrentCulture.CompareInfo.IndexOf(propertyValue, FilterValue, CompareOptions.IgnoreCase) >= 0;
			}
		*/
		return false;
	}
}
