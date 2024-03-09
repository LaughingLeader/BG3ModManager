﻿using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Windows.Input;

namespace DivinityModManager.Models.View;
public abstract class TreeViewEntry : ReactiveObject
{
	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsSelected { get; set; }

	public ObservableCollectionExtended<TreeViewEntry> Children { get; }

	public abstract object ViewModel { get; }

	public ICommand ToggleCommand { get; }

	public TreeViewEntry()
	{
		Children = [];

		ToggleCommand = ReactiveCommand.Create(() => IsExpanded = !IsExpanded);
	}
}
