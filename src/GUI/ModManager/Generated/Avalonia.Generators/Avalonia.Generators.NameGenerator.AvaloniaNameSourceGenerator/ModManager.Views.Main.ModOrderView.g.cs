﻿// <auto-generated />

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModManager.Views.Main
{
    partial class ModOrderView
    {
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.ReactiveUI.ViewModelViewHost CommandBar;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.Grid ActiveModsGrid;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::ModManager.Views.Mods.ModListView ActiveModsList;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::ModManager.Views.Mods.ModListView OverrideModsList;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::ModManager.Views.Mods.ModListView InactiveModsList;

        /// <summary>
        /// Wires up the controls and optionally loads XAML markup and attaches dev tools (if Avalonia.Diagnostics package is referenced).
        /// </summary>
        /// <param name="loadXaml">Should the XAML be loaded into the component.</param>

        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void InitializeComponent(bool loadXaml = true)
        {
            if (loadXaml)
            {
                AvaloniaXamlLoader.Load(this);
            }

            var __thisNameScope__ = this.FindNameScope();
            CommandBar = __thisNameScope__?.Find<global::Avalonia.ReactiveUI.ViewModelViewHost>("CommandBar");
            ActiveModsGrid = __thisNameScope__?.Find<global::Avalonia.Controls.Grid>("ActiveModsGrid");
            ActiveModsList = __thisNameScope__?.Find<global::ModManager.Views.Mods.ModListView>("ActiveModsList");
            OverrideModsList = __thisNameScope__?.Find<global::ModManager.Views.Mods.ModListView>("OverrideModsList");
            InactiveModsList = __thisNameScope__?.Find<global::ModManager.Views.Mods.ModListView>("InactiveModsList");
        }
    }
}
