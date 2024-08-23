﻿// <auto-generated />

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModManager.Views.Mods
{
    partial class ModListView
    {
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.Expander FilterExpander;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBox FilterTextBox;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TreeDataGrid ModsTreeDataGrid;

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
            FilterExpander = __thisNameScope__?.Find<global::Avalonia.Controls.Expander>("FilterExpander");
            FilterTextBox = __thisNameScope__?.Find<global::Avalonia.Controls.TextBox>("FilterTextBox");
            ModsTreeDataGrid = __thisNameScope__?.Find<global::Avalonia.Controls.TreeDataGrid>("ModsTreeDataGrid");
        }
    }
}
