using System.Windows.Media.Imaging;

namespace ModManager.Util;

public static class PropertyHelpers
{
	public static BitmapImage UriToImage(Uri uri)
	{
		if (uri != null)
		{
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.UriSource = uri;
			bitmap.EndInit();
			return bitmap;
		}
		return null;
	}
}
