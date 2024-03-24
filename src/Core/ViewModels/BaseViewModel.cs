using ReactiveUI;

using System.Reactive.Disposables;

namespace ModManager.ViewModels;

public class BaseViewModel : ReactiveObject, IDisposable
{
	public CompositeDisposable Disposables { get; private set; }

	public void Dispose()
	{
		this.Disposables?.Dispose();
	}

	public BaseViewModel()
	{
		Disposables = new CompositeDisposable();
	}
}
