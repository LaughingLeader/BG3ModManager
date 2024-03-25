using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Wpf;

using ModManager.Controls;
using ModManager.Models.View;
using ModManager.ViewModels;
using ModManager.ViewModels.Main;
using ModManager.Views;
using ModManager.Views.Main;
using ModManager.Views.StatsValidator;
using ModManager.Windows;

using ReactiveUI;

using System.Windows.Controls;

namespace ModManager;

public class ViewLocator : StrongViewLocator, ReactiveUI.IViewLocator
{
	private void RegisterConstant<TViewModel, TView>() where TViewModel : ReactiveObject where TView : Control, new()
	{
		Register<TViewModel>(new ViewDefinition(typeof(TView), () => AppServices.Get<TView>()));
	}

	public ViewLocator()
	{
		//RegisterConstant<MainWindowViewModel, MainWindow>();
		//RegisterConstant<AboutWindowViewModel, AboutWindow>();
		//RegisterConstant<CollectionDownloadWindowViewModel, CollectionDownloadWindow>();
		//RegisterConstant<AppUpdateWindowViewModel, AppUpdateWindow>();
		//RegisterConstant<ExportOrderToArchiveViewModel, ExportOrderToArchiveView>();
		//RegisterConstant<HelpWindowViewModel, HelpWindow>();
		//RegisterConstant<ModPropertiesWindowViewModel, ModPropertiesWindow>();
		//RegisterConstant<NxmDownloadWindowViewModel, NxmDownloadWindow>();
		//RegisterConstant<SettingsWindowViewModel, SettingsWindow>();
		//RegisterConstant<VersionGeneratorViewModel, VersionGeneratorWindow>();

		RegisterConstant<DeleteFilesViewModel, DeleteFilesConfirmationView>();
		RegisterConstant<ModOrderViewModel, ModOrderView>();
		RegisterConstant<ModUpdatesViewModel, ModUpdatesLayout>();

		Register<DownloadActivityBarViewModel, DownloadActivityBar>();
		Register<StatsValidatorFileResults, StatsValidatorFileEntryView>();
		Register<StatsValidatorErrorEntry, StatsValidatorEntryView>();
		Register<StatsValidatorLineText, StatsValidatorLineView>();
	}

	public IViewFor ResolveView<T>(T viewModel, string contract = null) => (IViewFor)Create(viewModel);
}
