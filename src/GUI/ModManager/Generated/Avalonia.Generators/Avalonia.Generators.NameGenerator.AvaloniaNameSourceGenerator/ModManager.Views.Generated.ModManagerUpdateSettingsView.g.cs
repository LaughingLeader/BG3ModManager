﻿// <auto-generated />

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModManager.Views.Generated
{
    partial class ModManagerUpdateSettingsView
    {
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock UpdateScriptExtenderTextBlock;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock UpdateGitHubModsTextBlock;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock UpdateNexusModsTextBlock;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock UpdateModioModsTextBlock;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock NexusModsAPIKeyTextBlock;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock ModioAPIKeyTextBlock;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock MinimumUpdateTimePeriodTextBlock;
        [global::System.CodeDom.Compiler.GeneratedCode("Avalonia.Generators.NameGenerator.InitializeComponentCodeGenerator", "11.2.0.0")]
        internal global::Avalonia.Controls.TextBlock AllowAdultContentTextBlock;

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
            UpdateScriptExtenderTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("UpdateScriptExtenderTextBlock");
            UpdateGitHubModsTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("UpdateGitHubModsTextBlock");
            UpdateNexusModsTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("UpdateNexusModsTextBlock");
            UpdateModioModsTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("UpdateModioModsTextBlock");
            NexusModsAPIKeyTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("NexusModsAPIKeyTextBlock");
            ModioAPIKeyTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("ModioAPIKeyTextBlock");
            MinimumUpdateTimePeriodTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("MinimumUpdateTimePeriodTextBlock");
            AllowAdultContentTextBlock = __thisNameScope__?.Find<global::Avalonia.Controls.TextBlock>("AllowAdultContentTextBlock");
        }
    }
}