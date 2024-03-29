namespace ModManager.ViewModels;

public interface IClosableViewModel
{
	bool IsVisible { get; set; }
	RxCommandUnit CloseCommand { get; }
}