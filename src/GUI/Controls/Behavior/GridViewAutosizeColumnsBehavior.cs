using DivinityModManager.Models.Mod;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DivinityModManager.Controls.Behavior;

public class GridViewAutoSizeColumnsBehavior
{
	public static bool GetGridViewAutoSizeColumns(ListView listView)
	{
		return (bool)listView.GetValue(GridViewAutoSizeColumnsProperty);
	}

	public static void SetGridViewAutoSizeColumns(ListView listView, bool value)
	{
		listView.SetValue(GridViewAutoSizeColumnsProperty, value);
	}

	public static readonly DependencyProperty GridViewAutoSizeColumnsProperty =
		DependencyProperty.RegisterAttached(
		"AutoSizeColumns",
		typeof(bool),
		typeof(GridViewAutoSizeColumnsBehavior),
		new UIPropertyMetadata(false, OnAutoSizeColumnsChanged));

	static void OnAutoSizeColumnsChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
	{
		if (depObj is ListView listView)
		{
			if (e.NewValue is bool enabled)
			{
				if (enabled)
				{
					listView.Loaded += OnDataChangedChanged;
					//listView.SizeChanged += OnGridViewSizeChanged;
				}
				else
				{
					listView.Loaded -= OnDataChangedChanged;
					//listView.SizeChanged -= OnGridViewSizeChanged;
				}
			}
		}
	}

	static void OnGridViewSizeChanged(object sender, RoutedEventArgs e)
	{
		if (sender is ListView listView)
		{
			if (listView.View is GridView gridView)
			{
				if (gridView.Columns.Count >= 2)
				{
					// take into account vertical scrollbar
					var actualWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;

					for (var i = 2; i < gridView.Columns.Count; i++)
					{
						actualWidth -= gridView.Columns[i].ActualWidth;
					}

					if (actualWidth > 0 && gridView.Columns.Count >= 2)
					{
						gridView.Columns[1].Width = actualWidth;
					}
				}
			}
		}
	}

	static void OnDataChangedChanged(object sender, EventArgs e)
	{
		if (sender is ListView listView)
		{
			if (listView.View is GridView gridView)
			{
				if (gridView.Columns.Count >= 2)
				{
					if (listView.ItemsSource is IEnumerable<DivinityModData> mods && mods.Count() > 0)
					{
						var longestName = mods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
						gridView.Columns[1].Width = MeasureText(listView, longestName,
							listView.FontFamily,
							listView.FontStyle,
							listView.FontWeight,
							listView.FontStretch,
							listView.FontSize).Width;
					}
				}
			}
		}
	}

	private static Size MeasureTextSize(Visual target, string text, FontFamily fontFamily, FontStyle fontStyle,
		FontWeight fontWeight, FontStretch fontStretch, double fontSize)
	{
		var typeFace = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
		var ft = new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black, VisualTreeHelper.GetDpi(target).PixelsPerDip);
		return new Size(ft.Width, ft.Height);
	}

	private static Size MeasureText(Visual target, string text,
		FontFamily fontFamily,
		FontStyle fontStyle,
		FontWeight fontWeight,
		FontStretch fontStretch, double fontSize)
	{
		Typeface typeface = new(fontFamily, fontStyle, fontWeight, fontStretch);
		GlyphTypeface glyphTypeface;

		if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
		{
			return MeasureTextSize(target, text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
		}

		double totalWidth = 0;
		double height = 0;

		for (int n = 0; n < text.Length; n++)
		{
			ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

			double width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;

			double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

			if (glyphHeight > height)
			{
				height = glyphHeight;
			}

			totalWidth += width;
		}

		return new Size(totalWidth, height);
	}
}
