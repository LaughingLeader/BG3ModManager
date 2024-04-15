﻿// <auto-generated />

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModManager.Views.Main
{
    partial class ModOrderView
    {
        internal global::Avalonia.ReactiveUI.ViewModelViewHost CommandBar;
        internal global::Avalonia.Controls.Grid ActiveModsGrid;
        internal global::ModManager.Views.Mods.ModListView ActiveModsList;
        internal global::ModManager.Views.Mods.ModListView OverrideModsList;
        internal global::ModManager.Views.Mods.ModListView InactiveModsList;

        /// <summary>
        /// Wires up the controls and optionally loads XAML markup and attaches dev tools (if Avalonia.Diagnostics package is referenced).
        /// </summary>
        /// <param name="loadXaml">Should the XAML be loaded into the component.</param>

        public void InitializeComponent(bool loadXaml = true)
        {
            if (loadXaml)
            {
                AvaloniaXamlLoader.Load(this);
            }

            CommandBar = this.FindNameScope()?.Find<global::Avalonia.ReactiveUI.ViewModelViewHost>("CommandBar");
            ActiveModsGrid = this.FindNameScope()?.Find<global::Avalonia.Controls.Grid>("ActiveModsGrid");
            ActiveModsList = this.FindNameScope()?.Find<global::ModManager.Views.Mods.ModListView>("ActiveModsList");
            OverrideModsList = this.FindNameScope()?.Find<global::ModManager.Views.Mods.ModListView>("OverrideModsList");
            InactiveModsList = this.FindNameScope()?.Find<global::ModManager.Views.Mods.ModListView>("InactiveModsList");
        }
    }
}
