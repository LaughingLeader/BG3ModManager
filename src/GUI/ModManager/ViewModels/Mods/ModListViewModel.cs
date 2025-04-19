﻿using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;

using DynamicData;
using DynamicData.Binding;

using ModManager.Models;
using ModManager.Models.Mod;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ModManager.ViewModels.Mods;
public partial class ModListViewModel : ReactiveObject
{
	[GeneratedRegex("@([^\\s]+?)([\\s]+)([^@\\s]*)")]
	private static partial Regex FilterPropertyRe();
	[GeneratedRegex("@([^\\s]+?)([\\s\"]+)([^@\"]*)")]
	private static partial Regex FilterPropertyPatternWithQuotesRe();

	private readonly ICollection<IModEntry> _mods;
	private readonly ModEntryTreeDataGridRowSelectionModel _rowSelection;

	public HierarchicalTreeDataGridSource<IModEntry> Mods { get; }

	private static readonly Regex s_filterPropertyPattern = FilterPropertyRe();
	private static readonly Regex s_filterPropertyPatternWithQuotes = FilterPropertyPatternWithQuotesRe();

	[Reactive] public string Title { get; set; }
	[Reactive] public string? FilterInputText { get; set; }
	[Reactive] public bool IsFilterEnabled { get; set; }
	[Reactive] public int TotalMods { get; private set; }
	[Reactive] public int TotalModsHidden { get; private set; }
	[Reactive] public int TotalModsSelected { get; private set; }

	[Reactive] public bool IsFocused { get; set; }
	[Reactive] public bool IsKeyboardFocusWithin { get; set; }

	[Reactive] public IModEntry? SelectedItem { get; set; }

	[ObservableAsProperty] public string? FilterPlaceholderText { get; }
	[ObservableAsProperty] public string? FilterResultText { get; }
	[ObservableAsProperty] public bool HasAnyFocus { get; }

	public RxCommandUnit FocusCommand { get; }

	public void FilterMods(string? searchText)
	{
		foreach (var m in _mods)
		{
			m.IsHidden = false;
		}

		if (!string.IsNullOrWhiteSpace(searchText))
		{
			if (searchText.IndexOf('@') > -1)
			{
				var remainingSearch = searchText;
				List<DivinityModFilterData> searchProps = [];

				MatchCollection matches;

				if (searchText.IndexOf('\"') > -1)
				{
					matches = s_filterPropertyPatternWithQuotes.Matches(searchText);
				}
				else
				{
					matches = s_filterPropertyPattern.Matches(searchText);
				}

				if (matches.Count > 0)
				{
					foreach (var match in matches.Cast<Match>())
					{
						if (match.Success)
						{
							var prop = match.Groups[1]?.Value;
							var value = match.Groups[3]?.Value;
							if (String.IsNullOrEmpty(value)) value = "";
							if (!String.IsNullOrWhiteSpace(prop))
							{
								searchProps.Add(new DivinityModFilterData()
								{
									FilterProperty = prop,
									FilterValue = value
								});

								remainingSearch = remainingSearch.Replace(match.Value, "");
							}
						}
					}
				}

				remainingSearch = remainingSearch.Replace("\"", "");

				//If no Name property is specified, use the remaining unmatched text for that
				if (!String.IsNullOrWhiteSpace(remainingSearch) && !searchProps.Any(f => f.PropertyContains("Name")))
				{
					remainingSearch = remainingSearch.Trim();
					searchProps.Add(new DivinityModFilterData()
					{
						FilterProperty = "Name",
						FilterValue = remainingSearch
					});
				}

				foreach (var mod in _mods)
				{
					//@Mode GM @Author Leader
					var totalMatches = 0;
					foreach (var f in searchProps)
					{
						if (f.Match(mod))
						{
							totalMatches += 1;
						}
					}

					if (totalMatches < searchProps.Count)
					{
						mod.IsHidden = true;
					}
				}
			}
			else
			{
				foreach (var m in _mods)
				{
					if (m.DisplayName.IsValid())
					{
						var matchIndex = CultureInfo.CurrentCulture.CompareInfo.IndexOf(m.DisplayName, searchText, CompareOptions.IgnoreCase);
						
						if (matchIndex <= -1)
						{
							m.IsHidden = true;
						}
					}
				}
			}
		}
	}

	private static string ToFilterResultText(ValueTuple<int, int, int, string?, bool> x)
	{
		var (total, totalHidden, totalSelected, filterText, isEnabled) = x;
		if (total <= 0 || !isEnabled) return string.Empty;

		List<string> texts = [];
		if (!string.IsNullOrWhiteSpace(filterText))
		{
			var matched = Math.Max(0, total - totalHidden);
			texts.Add($"{matched} Matched");
			if(totalHidden > 0) texts.Add($"{totalHidden} Hidden");
		}
		if(totalSelected > 0) texts.Add($"{total} Selected");
		
		return string.Join(", ", texts);
	}

	private void CountMods(NotifyCollectionChangedEventArgs e)
	{
		var total = 0;
		var totalHidden = 0;
		var totalSelected = 0;
		foreach (var mod in _mods)
		{
			total++;
			if (mod.IsHidden) totalHidden++;
			if (mod.IsSelected) totalSelected++;
		}
		TotalMods = total;
		TotalModsHidden = totalHidden;
		TotalModsSelected = totalSelected;
	}

	public void UpdateIndexes()
	{
		var index = 0;
		foreach(var mod in Mods.Items)
		{
			mod.Index = index;
			index++;
		}
	}

	private void UpdateSelection(TreeSelectionModelSelectionChangedEventArgs<IModEntry> e)
	{
		if(e.SelectedItems.Count > 0)
		{
			SelectedItem = e.SelectedItems[0];
		}
		else
		{
			SelectedItem = null;
		}

		foreach (var item in e.SelectedItems)
		{
			if (item != null) item.IsSelected = true;
		}

		foreach (var item in e.DeselectedItems)
		{
			if (item != null) item.IsSelected = false;
		}
	}

	public ModListViewModel(HierarchicalTreeDataGridSource<IModEntry> treeGridSource,
		ICollection<IModEntry> collection,
		INotifyCollectionChanged observedCollection,
		IObservable<IChangeSet<IModEntry>> connection,
		string title = "")
	{
		_mods = collection;
		Mods = treeGridSource;
		Title = title;

		_rowSelection = new ModEntryTreeDataGridRowSelectionModel(treeGridSource) { SingleSelect = false };
		treeGridSource.Selection = _rowSelection;

		Observable.FromEvent<EventHandler<TreeSelectionModelSelectionChangedEventArgs<IModEntry>>?, TreeSelectionModelSelectionChangedEventArgs<IModEntry>>(
			h => (sender, e) => h(e),
			h => _rowSelection.SelectionChanged += h,
			h => _rowSelection.SelectionChanged -= h
		).ObserveOn(RxApp.MainThreadScheduler)
		.Subscribe(UpdateSelection);

		Observable.FromEvent<NotifyCollectionChangedEventHandler?, NotifyCollectionChangedEventArgs>(
			h => (sender, e) => h(e),
			h => observedCollection.CollectionChanged += h,
			h => observedCollection.CollectionChanged -= h
		).Throttle(TimeSpan.FromMilliseconds(50))
		.ObserveOn(RxApp.MainThreadScheduler)
		.Subscribe(CountMods);

		this.WhenAnyValue(x => x.TotalMods, x => x.TotalModsHidden, x => x.TotalModsSelected,
			x => x.FilterInputText, x => x.IsFilterEnabled)
			.Select(ToFilterResultText)
			.ToUIProperty(this, x => x.FilterResultText);

		//Disable/enable filtering depending on the expander
		this.WhenAnyValue(x => x.IsFilterEnabled, x => x.FilterInputText)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Select(x => x.Item1 ? x.Item2 : null)
			.Subscribe(FilterMods);

		this.WhenAnyValue(x => x.Title).Select(x => $"Filter {x}").ToUIProperty(this, x => x.FilterPlaceholderText);

		this.WhenAnyValue(x => x.IsFocused, x => x.IsKeyboardFocusWithin).Select(x => x.Item1 || x.Item2).ToUIPropertyImmediate(this, x => x.HasAnyFocus);

		FocusCommand = ReactiveCommand.Create(() => { });
	}
}

public class DesignModListViewModel : ModListViewModel
{
	private static class DesignModListViewModelDataSource
	{
		public static readonly ObservableCollectionExtended<IModEntry> ReadonlyMods = [];

		public static ObservableCollectionExtended<IModEntry> Mods { get; }
		public static HierarchicalTreeDataGridSource<IModEntry> DataSource { get; }
		public static IObservable<IChangeSet<IModEntry>> ModsConnection { get; }

		static DesignModListViewModelDataSource()
		{
			Mods = [];
			Mods.AddRange(ModelGlobals.TestMods);

			Mods.ToObservableChangeSet()
				.AutoRefresh(x => x.IsHidden)
				.Filter(x => !x.IsHidden)
				.ObserveOn(RxApp.MainThreadScheduler).Bind(ReadonlyMods).Subscribe();

			ModsConnection = Mods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);

			DataSource = new HierarchicalTreeDataGridSource<IModEntry>(ReadonlyMods)
			{
				Columns =
				{
					//Avalonia.Controls.Models.TreeDataGrid.
					new TextColumn<IModEntry, int>("Index", x => x.Index, GridLength.Auto),
					new HierarchicalExpanderColumn<IModEntry>(
					new TextColumn<IModEntry, string>("Name", x => x.DisplayName, GridLength.Star),
					x => x.Children, x => x.Children != null && x.Children.Count > 0, x => x.IsExpanded),
					new TextColumn<IModEntry, string>("Version", x => x.Version, GridLength.Auto),
					new TextColumn<IModEntry, string>("Author", x => x.Author, GridLength.Auto),
					new TextColumn<IModEntry, string>("Last Updated", x => x.LastUpdated, GridLength.Auto),
				}
			};
		}
	}

	public DesignModListViewModel() : base(DesignModListViewModelDataSource.DataSource,
		DesignModListViewModelDataSource.Mods,
		DesignModListViewModelDataSource.ReadonlyMods,
		DesignModListViewModelDataSource.ModsConnection, "Active")
	{

	}

	public DesignModListViewModel(HierarchicalTreeDataGridSource<IModEntry> treeGridSource,
		ICollection<IModEntry> collection,
		INotifyCollectionChanged observedCollection,
		IObservable<IChangeSet<IModEntry>> connection,
		string title = "") : base(treeGridSource, collection, observedCollection, connection, title)
	{

	}
}