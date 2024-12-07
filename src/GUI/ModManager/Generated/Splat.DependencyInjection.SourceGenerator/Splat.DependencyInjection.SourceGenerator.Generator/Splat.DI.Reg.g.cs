﻿
// <auto-generated />
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static partial void SetupIOCInternal(Splat.IDependencyResolver resolver) 
        {
            {
                global::System.Lazy<ModManager.Services.ModImportService> lazy = new global::System.Lazy<ModManager.Services.ModImportService>(() => new global::ModManager.Services.ModImportService((global::ModManager.IDialogService)resolver.GetService(typeof(global::ModManager.IDialogService))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Services.ModImportService>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Services.ModImportService));
            }
            {
                global::System.Lazy<ModManager.IDialogService> lazy = new global::System.Lazy<ModManager.IDialogService>(() => new global::ModManager.Services.DialogService((global::ModManager.IInteractionsService)resolver.GetService(typeof(global::ModManager.IInteractionsService))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.IDialogService>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.IDialogService));
            }
            {
                global::System.Lazy<ModManager.AppKeysService> lazy = new global::System.Lazy<ModManager.AppKeysService>(() => new global::ModManager.AppKeysService());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.AppKeysService>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.AppKeysService));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Main.MainWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Main.MainWindowViewModel>(() => new global::ModManager.ViewModels.Main.MainWindowViewModel((global::ModManager.IPathwaysService)resolver.GetService(typeof(global::ModManager.IPathwaysService)), (global::ModManager.ISettingsService)resolver.GetService(typeof(global::ModManager.ISettingsService)), (global::ModManager.Services.ModImportService)resolver.GetService(typeof(global::ModManager.Services.ModImportService)), (global::ModManager.IModManagerService)resolver.GetService(typeof(global::ModManager.IModManagerService)), (global::ModManager.IModUpdaterService)resolver.GetService(typeof(global::ModManager.IModUpdaterService)), (global::ModManager.INexusModsService)resolver.GetService(typeof(global::ModManager.INexusModsService)), (global::ModManager.IInteractionsService)resolver.GetService(typeof(global::ModManager.IInteractionsService)), (global::ModManager.IEnvironmentService)resolver.GetService(typeof(global::ModManager.IEnvironmentService)), (global::ModManager.IGlobalCommandsService)resolver.GetService(typeof(global::ModManager.IGlobalCommandsService)), (global::ModManager.IDialogService)resolver.GetService(typeof(global::ModManager.IDialogService))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Main.MainWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Main.MainWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Main.MainCommandBarViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Main.MainCommandBarViewModel>(() => new global::ModManager.ViewModels.Main.MainCommandBarViewModel());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Main.MainCommandBarViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Main.MainCommandBarViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Main.DeleteFilesViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Main.DeleteFilesViewModel>(() => new global::ModManager.ViewModels.Main.DeleteFilesViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Main.DeleteFilesViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Main.DeleteFilesViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Main.ModOrderViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Main.ModOrderViewModel>(() => new global::ModManager.ViewModels.Main.ModOrderViewModel((global::ModManager.ViewModels.Main.MainWindowViewModel)resolver.GetService(typeof(global::ModManager.ViewModels.Main.MainWindowViewModel)), (global::ModManager.IModManagerService)resolver.GetService(typeof(global::ModManager.IModManagerService)), (global::ModManager.IFileWatcherService)resolver.GetService(typeof(global::ModManager.IFileWatcherService)), (global::ModManager.IInteractionsService)resolver.GetService(typeof(global::ModManager.IInteractionsService)), (global::ModManager.IGlobalCommandsService)resolver.GetService(typeof(global::ModManager.IGlobalCommandsService)), (global::ModManager.IDialogService)resolver.GetService(typeof(global::ModManager.IDialogService)), (global::ModManager.Services.ModImportService)resolver.GetService(typeof(global::ModManager.Services.ModImportService)), (global::ModManager.ViewModels.Main.MainCommandBarViewModel)resolver.GetService(typeof(global::ModManager.ViewModels.Main.MainCommandBarViewModel))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Main.ModOrderViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Main.ModOrderViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Main.ModUpdatesViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Main.ModUpdatesViewModel>(() => new global::ModManager.ViewModels.Main.ModUpdatesViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Main.ModUpdatesViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Main.ModUpdatesViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Main.IProgressBarViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Main.IProgressBarViewModel>(() => new global::ModManager.ViewModels.Main.ProgressBarViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Main.IProgressBarViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Main.IProgressBarViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.SettingsWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.SettingsWindowViewModel>(() => new global::ModManager.ViewModels.SettingsWindowViewModel((global::ModManager.IInteractionsService)resolver.GetService(typeof(global::ModManager.IInteractionsService)), (global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.SettingsWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.SettingsWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.AboutWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.AboutWindowViewModel>(() => new global::ModManager.ViewModels.AboutWindowViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.AboutWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.AboutWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.AppUpdateWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.AppUpdateWindowViewModel>(() => new global::ModManager.ViewModels.AppUpdateWindowViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.AppUpdateWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.AppUpdateWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.CollectionDownloadWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.CollectionDownloadWindowViewModel>(() => new global::ModManager.ViewModels.CollectionDownloadWindowViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.CollectionDownloadWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.CollectionDownloadWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.HelpWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.HelpWindowViewModel>(() => new global::ModManager.ViewModels.HelpWindowViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.HelpWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.HelpWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.ModPropertiesWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.ModPropertiesWindowViewModel>(() => new global::ModManager.ViewModels.ModPropertiesWindowViewModel());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.ModPropertiesWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.ModPropertiesWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.NxmDownloadWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.NxmDownloadWindowViewModel>(() => new global::ModManager.ViewModels.NxmDownloadWindowViewModel((global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.NxmDownloadWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.NxmDownloadWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.StatsValidatorWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.StatsValidatorWindowViewModel>(() => new global::ModManager.ViewModels.StatsValidatorWindowViewModel((global::ModManager.IInteractionsService)resolver.GetService(typeof(global::ModManager.IInteractionsService)), (global::ModManager.IStatsValidatorService)resolver.GetService(typeof(global::ModManager.IStatsValidatorService)), (global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.StatsValidatorWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.StatsValidatorWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.VersionGeneratorViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.VersionGeneratorViewModel>(() => new global::ModManager.ViewModels.VersionGeneratorViewModel((global::ModManager.IGlobalCommandsService)resolver.GetService(typeof(global::ModManager.IGlobalCommandsService)), (global::ReactiveUI.IScreen)resolver.GetService(typeof(global::ReactiveUI.IScreen))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.VersionGeneratorViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.VersionGeneratorViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.ExportOrderToArchiveViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.ExportOrderToArchiveViewModel>(() => new global::ModManager.ViewModels.ExportOrderToArchiveViewModel());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.ExportOrderToArchiveViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.ExportOrderToArchiveViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Window.PakFileExplorerWindowViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Window.PakFileExplorerWindowViewModel>(() => new global::ModManager.ViewModels.Window.PakFileExplorerWindowViewModel((global::ModManager.IDialogService)resolver.GetService(typeof(global::ModManager.IDialogService)), (global::ModManager.IGlobalCommandsService)resolver.GetService(typeof(global::ModManager.IGlobalCommandsService))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Window.PakFileExplorerWindowViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Window.PakFileExplorerWindowViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.Settings.KeybindingsViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.Settings.KeybindingsViewModel>(() => new global::ModManager.ViewModels.Settings.KeybindingsViewModel());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.Settings.KeybindingsViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.Settings.KeybindingsViewModel));
            }
            {
                global::System.Lazy<ModManager.ViewModels.MessageBoxViewModel> lazy = new global::System.Lazy<ModManager.ViewModels.MessageBoxViewModel>(() => new global::ModManager.ViewModels.MessageBoxViewModel());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.ViewModels.MessageBoxViewModel>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.ViewModels.MessageBoxViewModel));
            }
            {
                global::System.Lazy<ModManager.Views.Main.MainCommandBar> lazy = new global::System.Lazy<ModManager.Views.Main.MainCommandBar>(() => new global::ModManager.Views.Main.MainCommandBar());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Views.Main.MainCommandBar>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Views.Main.MainCommandBar));
            }
            {
                global::System.Lazy<ModManager.Views.Main.DeleteFilesView> lazy = new global::System.Lazy<ModManager.Views.Main.DeleteFilesView>(() => new global::ModManager.Views.Main.DeleteFilesView());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Views.Main.DeleteFilesView>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Views.Main.DeleteFilesView));
            }
            {
                global::System.Lazy<ModManager.Views.Main.ModOrderView> lazy = new global::System.Lazy<ModManager.Views.Main.ModOrderView>(() => new global::ModManager.Views.Main.ModOrderView());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Views.Main.ModOrderView>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Views.Main.ModOrderView));
            }
            {
                global::System.Lazy<ModManager.Views.Main.ModUpdatesView> lazy = new global::System.Lazy<ModManager.Views.Main.ModUpdatesView>(() => new global::ModManager.Views.Main.ModUpdatesView());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Views.Main.ModUpdatesView>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Views.Main.ModUpdatesView));
            }
            {
                global::System.Lazy<ModManager.Views.MessageBoxView> lazy = new global::System.Lazy<ModManager.Views.MessageBoxView>(() => new global::ModManager.Views.MessageBoxView());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Views.MessageBoxView>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Views.MessageBoxView));
            }
            {
                global::System.Lazy<ModManager.Views.Main.ProgressBarView> lazy = new global::System.Lazy<ModManager.Views.Main.ProgressBarView>(() => new global::ModManager.Views.Main.ProgressBarView());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Views.Main.ProgressBarView>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Views.Main.ProgressBarView));
            }
            {
                global::System.Lazy<ModManager.Windows.SettingsWindow> lazy = new global::System.Lazy<ModManager.Windows.SettingsWindow>(() => new global::ModManager.Windows.SettingsWindow());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Windows.SettingsWindow>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Windows.SettingsWindow));
            }
            {
                global::System.Lazy<ModManager.Windows.ModPropertiesWindow> lazy = new global::System.Lazy<ModManager.Windows.ModPropertiesWindow>(() => new global::ModManager.Windows.ModPropertiesWindow());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Windows.ModPropertiesWindow>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Windows.ModPropertiesWindow));
            }
            {
                global::System.Lazy<ModManager.Windows.PakFileExplorerWindow> lazy = new global::System.Lazy<ModManager.Windows.PakFileExplorerWindow>(() => new global::ModManager.Windows.PakFileExplorerWindow());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Windows.PakFileExplorerWindow>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Windows.PakFileExplorerWindow));
            }
            {
                global::System.Lazy<ModManager.Windows.StatsValidatorWindow> lazy = new global::System.Lazy<ModManager.Windows.StatsValidatorWindow>(() => new global::ModManager.Windows.StatsValidatorWindow());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Windows.StatsValidatorWindow>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Windows.StatsValidatorWindow));
            }
            {
                global::System.Lazy<ModManager.Services.WindowManagerService> lazy = new global::System.Lazy<ModManager.Services.WindowManagerService>(() => new global::ModManager.Services.WindowManagerService((global::ModManager.Windows.MainWindow)resolver.GetService(typeof(global::ModManager.Windows.MainWindow))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<ModManager.Services.WindowManagerService>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::ModManager.Services.WindowManagerService));
            }
        }
    }
}