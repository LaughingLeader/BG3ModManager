using System.ComponentModel;
using System.Reflection;

namespace ModManager;

public static class EnumExtensions
{
	/// <summary>
	/// Get an enum's Description attribute value.
	/// </summary>
	public static string GetDescription(this Enum enumValue)
	{
		var member = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
		if (member != null)
		{
			return member.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
		}
		return "";
	}

	public static bool IsConfirmation(this InteractionMessageBoxType messageBoxType)
	{
		return messageBoxType.HasFlag(InteractionMessageBoxType.YesNo);
	}
}
