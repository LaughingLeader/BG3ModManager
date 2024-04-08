using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Models;
public class HotkeyCommand<TParam, TResult> : ReactiveCommand<TParam, TResult>
{
	[Reactive] public string? Id { get; set; }
	[Reactive] public string? DisplayName { get; set; }
	[Reactive] public string? ToolTip { get; set; }

	protected internal HotkeyCommand(Func<TParam, IObservable<(IObservable<TResult> Result, Action Cancel)>> execute,
		IObservable<bool>? canExecute,
		IScheduler? outputScheduler) : base(execute, canExecute, outputScheduler)
	{

	}

	protected internal HotkeyCommand(Func<TParam, IObservable<TResult>> execute,
		IObservable<bool>? canExecute,
		IScheduler? outputScheduler) : base(execute, canExecute, outputScheduler)
	{

	}
}
