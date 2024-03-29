namespace ModManager.ViewModels;
public abstract class BaseWindowViewModel : ReactiveObject
{
	[Reactive] public bool IsVisible { get; set; }
}
