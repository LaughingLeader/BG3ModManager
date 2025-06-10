using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Controls;

[TemplatePart("PART_Icon", typeof(ContentPresenter))]
public class IconExpander : Expander
{
	//protected override Type StyleKeyOverride => typeof(Expander);

	public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<IconExpander, object?>(nameof(Icon), null);

	public object? Icon
	{
		get => GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	static IconExpander()
	{
		IconProperty.Changed.AddClassHandler<IconExpander>((x, e) => x.IconChanged(e));
	}

	private void IconChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.OldValue is ILogical oldChild)
		{
			LogicalChildren.Remove(oldChild);
		}

		if (e.NewValue is ILogical newChild)
		{
			LogicalChildren.Add(newChild);
		}
	}
}
