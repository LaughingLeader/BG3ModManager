﻿// <auto-generated />

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModManager.Windows
{
    partial class ModPropertiesWindow
    {
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Material.Icons.Avalonia.MaterialIcon ModTypeIconControl;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.Button OKButton;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.Button CancelButton;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.Button ApplyButton;

        /// <summary>
        /// Wires up the controls and optionally loads XAML markup and attaches dev tools (if Avalonia.Diagnostics package is referenced).
        /// </summary>
        /// <param name="loadXaml">Should the XAML be loaded into the component.</param>
        /// <param name="attachDevTools">Should the dev tools be attached.</param>

        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void InitializeComponent(bool loadXaml = true, bool attachDevTools = true)
        {
            if (loadXaml)
            {
                AvaloniaXamlLoader.Load(this);
            }

#if DEBUG
            if (attachDevTools)
            {
                this.AttachDevTools();
            }
#endif

            var __thisNameScope__ = this.FindNameScope();
            ModTypeIconControl = __thisNameScope__?.Find<global::Material.Icons.Avalonia.MaterialIcon>("ModTypeIconControl");
            OKButton = __thisNameScope__?.Find<global::Avalonia.Controls.Button>("OKButton");
            CancelButton = __thisNameScope__?.Find<global::Avalonia.Controls.Button>("CancelButton");
            ApplyButton = __thisNameScope__?.Find<global::Avalonia.Controls.Button>("ApplyButton");
        }
    }
}