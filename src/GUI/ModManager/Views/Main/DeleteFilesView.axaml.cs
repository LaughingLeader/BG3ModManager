using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class DeleteFilesView : ReactiveUserControl<DeleteFilesViewModel>
{
    public DeleteFilesView()
    {
        InitializeComponent();
    }
}