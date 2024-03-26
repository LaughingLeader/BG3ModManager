using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ModManager.ViewModels.Main;

namespace ModManager;

public partial class ModOrderView : ReactiveUserControl<ModOrderViewModel>
{
    public ModOrderView()
    {
        InitializeComponent();
    }
}