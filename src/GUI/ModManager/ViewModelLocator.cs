using ModManager.ViewModels;
using ModManager.ViewModels.Main;

namespace ModManager;

public static class ViewModelLocator
{
	public static MainWindowViewModel Main => AppServices.Get<MainWindowViewModel>()!;
	public static DeleteFilesViewModel DeleteFiles => AppServices.Get<DeleteFilesViewModel>()!;
	public static ModOrderViewModel ModOrder => AppServices.Get<ModOrderViewModel>()!;
	public static ModUpdatesViewModel ModUpdates => AppServices.Get<ModUpdatesViewModel>()!;
	public static MainCommandBarViewModel CommandBar => AppServices.Get<MainCommandBarViewModel>()!;

	public static SettingsWindowViewModel Settings => AppServices.Get<SettingsWindowViewModel>()!;

	public static AboutWindowViewModel About => AppServices.Get<AboutWindowViewModel>()!;
	public static AppUpdateWindowViewModel AppUpdate => AppServices.Get<AppUpdateWindowViewModel>()!;
	public static CollectionDownloadWindowViewModel CollectionDownload => AppServices.Get<CollectionDownloadWindowViewModel>()!;
	public static HelpWindowViewModel Help => AppServices.Get<HelpWindowViewModel>()!;
	public static ModPropertiesWindowViewModel ModProperties => AppServices.Get<ModPropertiesWindowViewModel>()!;
	public static NxmDownloadWindowViewModel NxmDownload => AppServices.Get<NxmDownloadWindowViewModel>()!;
	public static StatsValidatorWindowViewModel StatsValidator => AppServices.Get<StatsValidatorWindowViewModel>()!;
	public static VersionGeneratorViewModel VersionGenerator => AppServices.Get<VersionGeneratorViewModel>()!;
	public static ExportOrderToArchiveViewModel ExportOrderToArchive => AppServices.Get<ExportOrderToArchiveViewModel>()!;
	public static MessageBoxViewModel MessageBox => AppServices.Get<MessageBoxViewModel>()!;
}
