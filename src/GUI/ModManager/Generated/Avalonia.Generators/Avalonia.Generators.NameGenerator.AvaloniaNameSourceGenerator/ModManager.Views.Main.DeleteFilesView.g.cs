﻿// <auto-generated />

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModManager.Views.Main
{
    partial class DeleteFilesView
    {
        internal global::Avalonia.Controls.TextBlock TestText;

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

            TestText = this.FindNameScope()?.Find<global::Avalonia.Controls.TextBlock>("TestText");
        }
    }
}
