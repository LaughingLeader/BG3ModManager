using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModManager.Controls;

/// <summary>
/// Image that switches to monochrome when disabled.
/// Source: https://www.engineeringsolutions.de/creating-an-automatic-adjusting-image-for-buttons-in-wpf/
/// </summary>
public class AutoGrayableImage : Image
{
	// References to original and grayscale ImageSources
	private ImageSource _sourceColor;
	private ImageSource _sourceGray;
	// Original and grayscale opacity masks
	private Brush _opacityMaskColor;
	private Brush _opacityMaskGray;

	static AutoGrayableImage()
	{
		DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoGrayableImage), new FrameworkPropertyMetadata(typeof(AutoGrayableImage)));
	}

	/// <summary>
	/// Overwritten to handle changes of IsEnabled, Source and OpacityMask properties
	/// </summary>
	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		if (e.Property.Name.Equals(nameof(IsEnabled)))
		{
			if (e.NewValue as bool? == false)
			{
				Source = _sourceGray;
				OpacityMask = _opacityMaskGray;
			}
			else if (e.NewValue as bool? == true)
			{
				Source = _sourceColor;
				OpacityMask = _opacityMaskColor;
			}
		}
		else if (e.Property.Name.Equals(nameof(Source)) &&  // only recache Source if it's the new one from outside
				 !ReferenceEquals(Source, _sourceColor) &&
				 !ReferenceEquals(Source, _sourceGray))
		{
			SetSources();
		}
		else if (e.Property.Name.Equals(nameof(OpacityMask)) && // only recache opacityMask if it's the new one from outside
				 !ReferenceEquals(OpacityMask, _opacityMaskColor) &&
				 !ReferenceEquals(OpacityMask, _opacityMaskGray))
		{
			_opacityMaskColor = OpacityMask;
		}

		base.OnPropertyChanged(e);
	}

	/// <summary>
	/// Caches original ImageSource, creates and caches grayscale ImageSource and grayscale opacity mask
	/// </summary>
	private void SetSources()
	{
		// If grayscale image cannot be created set grayscale source to original Source first
		_sourceGray = _sourceColor = Source;

		// Create Opacity Mask for grayscale image as FormatConvertedBitmap does not keep transparency info
		_opacityMaskGray = new ImageBrush(_sourceColor)
		{
			Opacity = 0.6
		};
		Uri uri = null;

		try
		{
			// Get the string Uri for the original image source first
			var stringUri = TypeDescriptor.GetConverter(Source).ConvertTo(Source, typeof(string)) as string;

			// Try to resolve it as an absolute Uri 
			if (!Uri.TryCreate(stringUri, UriKind.Absolute, out uri))
			{
				// Uri is relative => requested image is in the same assembly as this object
				stringUri = "pack://application:,,,/" + stringUri.TrimStart(new char[2] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
				uri = new Uri(stringUri);
			}

			// create and cache grayscale ImageSource
			_sourceGray = new FormatConvertedBitmap(new BitmapImage(uri), PixelFormats.Gray8, null, 0);
		}
		catch (Exception e)
		{
			//Debug.Fail("The Image used cannot be grayed out.", "Use BitmapImage or URI as a Source in order to allow gray scaling. Make sure the absolute Uri is used as relative Uri may sometimes resolve incorrectly.\n\nException: " + e.Message);
			DivinityApp.Log($"Error greying out image '{uri}'({Source}).\n\nException: {e.Message}");
		}
	}
}
