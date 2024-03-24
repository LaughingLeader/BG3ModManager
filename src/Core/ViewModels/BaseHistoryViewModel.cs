using ModManager.Models;

using Reactive.Bindings.Extensions;

using ReactiveHistory;

using ReactiveUI;

using System.Reactive.Disposables;
using System.Windows.Input;

namespace ModManager.ViewModels;

public interface IHistoryViewModel
{
	IHistory History { get; }

	void Undo();
	void Redo();
}

public static class HistoryViewModelExtensions
{
	public static void ChangeListWithHistory<T>(this IHistoryViewModel vm, IList<T> source, IList<T> oldValue, IList<T> newValue)
	{
		if (vm.History != null)
		{
			void undo() => source = oldValue;
			void redo() => source = newValue;
			vm.History.Snapshot(undo, redo);
			source = newValue;
		}
	}

	public static void AddWithHistory<T>(this IHistoryViewModel vm, IList<T> source, T item)
	{
		if (vm.History != null)
		{
			int index = source.Count;
			void redo() => source.Insert(index, item);
			void undo() => source.RemoveAt(index);
			vm.History.Snapshot(undo, redo);
			redo();
		}
	}

	public static void RemoveWithHistory<T>(this IHistoryViewModel vm, IList<T> source, T item)
	{
		if (vm.History != null)
		{
			int index = source.IndexOf(item);
			void redo() => source.RemoveAt(index);
			void undo() => source.Insert(index, item);
			vm.History.Snapshot(undo, redo);
			redo();
		}
	}

	public static void CreateSnapshot(this IHistoryViewModel vm, Action undo, Action redo)
	{
		vm.History?.Snapshot(undo, redo);
	}
}

public abstract class BaseHistoryViewModel : BaseHistoryObject, IHistoryViewModel, IDisposable
{
	public CompositeDisposable Disposables { get; internal set; }

	public ICommand UndoCommand { get; set; }
	public ICommand RedoCommand { get; set; }
	public ICommand ClearHistoryCommand { get; set; }

	public void Dispose()
	{
		this.Disposables?.Dispose();
	}

	public void Undo()
	{
		History.Undo();
	}

	public void Redo()
	{
		History.Redo();
	}

	public BaseHistoryViewModel()
	{
		Disposables = new CompositeDisposable();

		var history = new StackHistory().AddTo(Disposables);
		History = history;

		var undo = ReactiveCommand.Create(Undo, History.CanUndo);
		undo.Subscribe().DisposeWith(this.Disposables);
		UndoCommand = undo;

		var redo = ReactiveCommand.Create(Redo, History.CanRedo);
		redo.Subscribe().DisposeWith(this.Disposables);
		RedoCommand = redo;

		var clear = ReactiveCommand.Create(History.Clear, History.CanClear);
		clear.Subscribe().DisposeWith(this.Disposables);
		ClearHistoryCommand = clear;
	}
}
