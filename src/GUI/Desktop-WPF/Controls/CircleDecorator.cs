using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModManager.Controls;

public class CircleDecorator : Border
{
	protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
	{
		var width = this.ActualWidth;
		var height = this.ActualHeight;
		var a = width / 2;
		var b = height / 2;
		Point centerPoint = new(a, b);
		var thickness = this.BorderThickness.Left;
		EllipseGeometry ellipse = new(centerPoint, a, b);
		drawingContext.PushClip(ellipse);
		drawingContext.DrawGeometry(
			this.Background,
			new Pen(this.BorderBrush, thickness),
			ellipse);
	}

	protected override Size MeasureOverride(Size constraint)
	{
		return base.MeasureOverride(constraint);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var a = finalSize.Width / 2;
		var b = finalSize.Height / 2;
		var PI = 3.1415926;
		var x = a * Math.Cos(45 * PI / 180);
		var y = b * Math.Sin(45 * PI / 180);
		Rect rect = new(new Point(a - x, b - y), new Point(a + x, b + y));
		if (base.Child != null)
		{
			base.Child.Arrange(rect);
		}

		return finalSize;
	}
}
