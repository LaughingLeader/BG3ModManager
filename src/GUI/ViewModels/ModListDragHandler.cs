﻿using DivinityModManager.Models;

using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DivinityModManager.ViewModels;

public class ManualDragInfo : IDragInfo
{
	public DataFormat DataFormat { get; set; }
	public object Data { get; set; }
	public Point DragStartPosition { get; }
	public Point PositionInDraggedItem { get; }
	public DragDropEffects Effects { get; set; }
	public MouseButton MouseButton { get; set; }
	public System.Collections.IEnumerable SourceCollection { get; set; }
	public int SourceIndex { get; set; }
	public object SourceItem { get; set; }
	public System.Collections.IEnumerable SourceItems { get; set; }
	public CollectionViewGroup SourceGroup { get; set; }
	public UIElement VisualSource { get; set; }
	public UIElement VisualSourceItem { get; set; }
	public FlowDirection VisualSourceFlowDirection { get; set; }
	public object DataObject { get; set; }
	public Func<DependencyObject, object, DragDropEffects, DragDropEffects> DragDropHandler { get; set; }
	public DragDropKeyStates DragDropCopyKeyState { get; set; }

	public void RefreshSourceItems(object sender)
	{
		if (sender is not ItemsControl itemsControl)
		{
			return;
		}

		var selectedItems = itemsControl.GetSelectedItems().OfType<object>().Where(i => i != CollectionView.NewItemPlaceholder).ToList();
		this.SourceItems = selectedItems;

		if (selectedItems.Count <= 1 || this.SourceItem != null && !selectedItems.Contains(this.SourceItem))
		{
			this.SourceItems = Enumerable.Repeat(this.SourceItem, 1);
		}
	}
}

public class ModListDragHandler : DefaultDragHandler
{
	private MainWindowViewModel _viewModel;

	public ModListDragHandler(MainWindowViewModel vm) : base()
	{
		_viewModel = vm;
	}

	private IDragInfo _lastDragInfo;

	private static IDisposable _stopDraggingFallbackTask = null;

	public override void StartDrag(IDragInfo dragInfo)
	{
		if (dragInfo != null)
		{
			_lastDragInfo = dragInfo;
			dragInfo.Data = null;
			if (dragInfo.SourceCollection == _viewModel.ActiveMods)
			{
				var selected = _viewModel.ActiveMods.Where(x => x.IsSelected && x.Visibility == Visibility.Visible);
				dragInfo.Data = selected;
			}
			else if (dragInfo.SourceCollection == _viewModel.InactiveMods)
			{
				var selected = _viewModel.InactiveMods.Where(x => x.IsSelected && x.Visibility == Visibility.Visible && x.CanDrag);
				dragInfo.Data = selected;
			}
			if (dragInfo.Data != null)
			{
				_viewModel.IsDragging = true;
				_stopDraggingFallbackTask?.Dispose();
				_stopDraggingFallbackTask = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMinutes(1), () =>
				{
					_viewModel.IsDragging = false;
				});
			}
			dragInfo.Effects = dragInfo.Data != null ? DragDropEffects.Copy | DragDropEffects.Move : DragDropEffects.None;
		}
	}

	public override void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
	{
		_viewModel.IsDragging = false;
		_stopDraggingFallbackTask?.Dispose();
	}

	public override bool CanStartDrag(IDragInfo dragInfo)
	{
		if (!_viewModel.AllowDrop)
		{
			return false;
		}
		if (dragInfo.Data is ISelectable d && !d.CanDrag)
		{
			return false;
		}
		else if (dragInfo.Data is IEnumerable<DivinityModData> modData)
		{
			if (modData.All(x => !x.CanDrag))
			{
				return false;
			}
		}
		return true;
	}

	public override void DragCancelled()
	{
		_viewModel.IsDragging = false;
		if (_lastDragInfo != null)
		{
			_lastDragInfo.Effects = DragDropEffects.None;
		}
		_stopDraggingFallbackTask?.Dispose();
	}

	public override bool TryCatchOccurredException(Exception exception)
	{
		if (exception is COMException)
		{
			//dragError.Message.IndexOf("A drag operation is already in progress") > -1
			DragCancelled();
			return true;
		}
		return false;
	}
}
