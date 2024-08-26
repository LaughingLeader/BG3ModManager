﻿// <auto-generated />

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModManager.Views.Main
{
    partial class MainCommandBar
    {
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.ComboBox ProfileComboBox;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::ModManager.Controls.ComboBoxWithRightClick OrdersComboBox;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.ComboBox CampaignComboBox;

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
            ProfileComboBox = __thisNameScope__?.Find<global::Avalonia.Controls.ComboBox>("ProfileComboBox");
            OrdersComboBox = __thisNameScope__?.Find<global::ModManager.Controls.ComboBoxWithRightClick>("OrdersComboBox");
            CampaignComboBox = __thisNameScope__?.Find<global::Avalonia.Controls.ComboBox>("CampaignComboBox");
        }
    }
}