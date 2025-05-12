using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Controls;
public abstract class HideWindowBase<TViewModel> : ReactiveWindow<TViewModel> where TViewModel : class
{
	public HideWindowBase()
	{
#if DEBUG
		if (Design.IsDesignMode)
		{
			Background = Brushes.Black;
		}
#endif
		this.Closing += HideWindowBase_Closing;
	}

	private void HideWindowBase_Closing(object? sender, WindowClosingEventArgs e)
	{
		e.Cancel = true;
		this.Hide();
	}
}
