using ModManager.Models;
using ModManager.Models.Mod;

using GongSolutions.Wpf.DragDrop;

using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ModManager.ViewModels;

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
}

public class ModListDragHandler : DefaultDragHandler
{
	private readonly MainWindowViewModel _viewModel;

	public ModListDragHandler(MainWindowViewModel vm) : base()
	{
		_viewModel = vm;
	}

	private IDragInfo _lastDragInfo;

	public override void StartDrag(IDragInfo dragInfo)
	{
		if (dragInfo != null)
		{
			var modOrderVM = _viewModel.Views.ModOrder;

			_lastDragInfo = dragInfo;
			dragInfo.Data = null;
			if (dragInfo.SourceCollection == modOrderVM.ActiveMods)
			{
				var selected = modOrderVM.ActiveMods.Where(x => x.IsSelected && x.Visibility == Visibility.Visible);
				dragInfo.Data = selected;
			}
			else if (dragInfo.SourceCollection == modOrderVM.InactiveMods)
			{
				var selected = modOrderVM.InactiveMods.Where(x => x.IsSelected && x.Visibility == Visibility.Visible && x.CanDrag);
				dragInfo.Data = selected;
			}
			if (dragInfo.Data != null)
			{
				_viewModel.IsDragging = true;
			}
			dragInfo.Effects = dragInfo.Data != null ? DragDropEffects.Copy | DragDropEffects.Move : DragDropEffects.None;
		}
	}

	public override void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
	{
		_viewModel.IsDragging = false;
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
