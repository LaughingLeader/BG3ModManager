using ModManager.ViewModels;
using ModManager.ViewModels.Main;
using ModManager.ViewModels.Mods;
using ModManager.ViewModels.Settings;
using ModManager.ViewModels.Window;

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
	public static NexusModsCollectionDownloadWindowViewModel CollectionDownload => AppServices.Get<NexusModsCollectionDownloadWindowViewModel>()!;
	public static HelpWindowViewModel Help => AppServices.Get<HelpWindowViewModel>()!;
	public static ModPropertiesWindowViewModel ModProperties => AppServices.Get<ModPropertiesWindowViewModel>()!;
	public static NxmDownloadWindowViewModel NxmDownload => AppServices.Get<NxmDownloadWindowViewModel>()!;
	public static StatsValidatorWindowViewModel StatsValidator => AppServices.Get<StatsValidatorWindowViewModel>()!;
	public static VersionGeneratorViewModel VersionGenerator => AppServices.Get<VersionGeneratorViewModel>()!;
	public static ExportOrderToArchiveViewModel ExportOrderToArchive => AppServices.Get<ExportOrderToArchiveViewModel>()!;
	public static MessageBoxViewModel MessageBox => AppServices.Get<MessageBoxViewModel>()!;
	public static ModPickerViewModel ModPicker => AppServices.Get<ModPickerViewModel>()!;
	public static IProgressBarViewModel Progress => AppServices.Get<IProgressBarViewModel>()!;
	public static KeybindingsViewModel Keybindings => AppServices.Get<KeybindingsViewModel>()!;
	public static FooterViewModel Footer => AppServices.Get<FooterViewModel>()!;

	public static PakFileExplorerWindowViewModel PakFileExplorer => AppServices.Get<PakFileExplorerWindowViewModel>()!;
}
