using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ModManager.Converters;

internal class UriToBitmapImageConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Uri uri)
		{
			try
			{
				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.UriSource = uri;
				bitmap.EndInit();
				return bitmap;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Failed to create BitmapImage from '{uri}':\n{ex}");
			}
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return "";
	}
}
