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

namespace ModManager;

public class ViewLocator : StrongViewLocator, ReactiveUI.IViewLocator
{
	public ViewLocator()
	{
		Register<MainWindowViewModel, MainWindow>();
		Register<AboutWindowViewModel, AboutWindow>();
		Register<CollectionDownloadWindowViewModel, CollectionDownloadWindow>();
		Register<AppUpdateWindowViewModel, AppUpdateWindow>();
		Register<DownloadActivityBarViewModel, DownloadActivityBar>();
		Register<ExportOrderToArchiveViewModel, ExportOrderToArchiveView>();
		Register<HelpWindowViewModel, HelpWindow>();
		Register<ModPropertiesWindowViewModel, ModPropertiesWindow>();
		Register<NxmDownloadWindowViewModel, NxmDownloadWindow>();
		Register<SettingsWindowViewModel, SettingsWindow>();
		Register<VersionGeneratorViewModel, VersionGeneratorWindow>();
		Register<DeleteFilesViewModel, DeleteFilesConfirmationView>();
		Register<ModOrderViewModel, ModOrderView>();
		Register<ModUpdatesViewModel, ModUpdatesLayout>();

		Register<StatsValidatorFileResults, StatsValidatorFileEntryView>();
		Register<StatsValidatorErrorEntry, StatsValidatorEntryView>();
		Register<StatsValidatorLineText, StatsValidatorLineView>();
	}

	public IViewFor ResolveView<T>(T viewModel, string contract = null) => (IViewFor)Create(viewModel);
}
