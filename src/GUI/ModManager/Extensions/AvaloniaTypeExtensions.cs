using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;

namespace ModManager;
public static class AvaloniaTypeExtensions
{
	public static FilePickerFileType ToFilePickerType(this FileTypeFilter filter)
	{
		var first = filter.Extensions.FirstOrDefault() ?? "";
		if (first == "*" || first == "*.*") return FilePickerFileTypes.All;

		if (FilePickerFileTypes.TextPlain.Patterns != null && filter.Extensions.All(x => FilePickerFileTypes.TextPlain.Patterns.Contains(x)))
		{
			return FilePickerFileTypes.TextPlain;
		}

		return new FilePickerFileType(filter.GetDisplayName())
		{
			Patterns = filter.Extensions,
			AppleUniformTypeIdentifiers = ["public.item"],
			MimeTypes = ["*/*"]
		};
	}

	public static NotificationType ToNotificationType(this AlertType alertType)
	{
		return alertType switch
		{
			AlertType.Info => NotificationType.Information,
			AlertType.Success => NotificationType.Success,
			AlertType.Warning => NotificationType.Warning,
			AlertType.Danger => NotificationType.Error,
			_ => NotificationType.Information
		};
	}
}
