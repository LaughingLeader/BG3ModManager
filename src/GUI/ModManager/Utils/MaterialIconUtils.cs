using Avalonia.Media;

using Material.Icons;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Utils;
public static class MaterialIconUtils
{
	public static MaterialIconKind ExtensionToIconKind(string? ext)
	{
		return ext switch
		{
			".dds" => MaterialIconKind.TextureBox,
			".gif" => MaterialIconKind.FileGifBox,
			".gr2" or ".dae" => MaterialIconKind.BlenderSoftware,
			".jpeg" => MaterialIconKind.FileJpegBox,
			".jpg" => MaterialIconKind.FileJpgBox,
			".loca" => MaterialIconKind.Language,
			".lsj" or ".json" => MaterialIconKind.CodeJson,
			".lsx" or ".xml" => MaterialIconKind.FileXmlBox,
			".lua" => MaterialIconKind.LanguageLua,
			".md" => MaterialIconKind.LanguageMarkdown,
			".pak" => MaterialIconKind.PackageVariantClosed,
			".pdf" => MaterialIconKind.FilePdfBox,
			".png" => MaterialIconKind.FilePngBox,
			".tiff" => MaterialIconKind.FileImage,
			".txt" => MaterialIconKind.Text,
			".xaml" => MaterialIconKind.LanguageXaml,
			_ => MaterialIconKind.File,
		};
	}

	public static MaterialIconKind ExtensionToModIconKind(string? ext)
	{
		return ext switch
		{
			".lsx" or ".xml" => MaterialIconKind.Tools,
			".pak" => MaterialIconKind.PackageVariantClosed,
			_ => MaterialIconKind.File,
		};
	}

	public static IBrush ExtensionToIconBrush(string? ext)
	{
		return ext switch
		{
			".dds" => Brushes.Red,
			".gif" => Brushes.Salmon,
			".gr2" or ".dae" => Brushes.DarkOrange,
			".jpeg" => Brushes.Coral,
			".jpg" => Brushes.Coral,
			".loca" => Brushes.PowderBlue,
			".lsj" or ".json" => Brushes.GreenYellow,
			".lsx" or ".xml" => Brushes.MediumSeaGreen,
			".lua" => Brushes.RoyalBlue,
			".md" => Brushes.MediumPurple,
			".pak" => Brushes.Aqua,
			".pdf" => Brushes.Tomato,
			".png" => Brushes.Firebrick,
			".tiff" => Brushes.MediumOrchid,
			".txt" => Brushes.PaleGreen,
			".xaml" => Brushes.MediumSeaGreen,
			_ => Brushes.White,
		};
	}
}
