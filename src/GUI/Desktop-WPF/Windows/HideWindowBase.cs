using AdonisUI.Controls;

using ModManager.ViewModels;

using ReactiveUI;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModManager.Windows;

public class HideWindowBase<TViewModel> : AdonisWindow, IViewFor<TViewModel> where TViewModel : BaseWindowViewModel
{
	/// <summary>
	/// The view model dependency property.
	/// </summary>
	public static readonly DependencyProperty ViewModelProperty =
		DependencyProperty.Register("ViewModel",
		typeof(TViewModel),
		typeof(HideWindowBase<TViewModel>),
		new PropertyMetadata(null));

	/// <summary>
	/// Gets the binding root view model.
	/// </summary>
	public TViewModel BindingRoot => ViewModel;

	/// <inheritdoc/>
	public TViewModel ViewModel
	{
		get => (TViewModel)GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	/// <inheritdoc/>
	object IViewFor.ViewModel
	{
		get => ViewModel;
		set => ViewModel = (TViewModel)value;
	}

	public bool HideOnEscapeKey { get; set; } = true;

	public HideWindowBase()
	{
		Closing += OnClosing;
		KeyDown += (o, e) =>
		{
			if (HideOnEscapeKey && !e.Handled && e.Key == Key.Escape)
			{
				if (Keyboard.FocusedElement == null || Keyboard.FocusedElement.GetType() != typeof(TextBox))
				{
					Hide();
				}
			}
		};

		this.WhenActivated(d =>
		{
			this.Events().IsVisibleChanged.Select(x => (bool)x.NewValue).BindTo(this, x => x.ViewModel.IsVisible);
		});

		Hide();
	}

	public virtual void OnClosing(object sender, CancelEventArgs e)
	{
		e.Cancel = true;
		Hide();
	}
}
