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

	public static T[] IndexToEnumArray<T>() where T : Enum
	{
		var enumType = typeof(T);
		var names = Enum.GetNames(enumType).ToList();
		T[] result = new T[names.Count];
		var i = 0;
		foreach(string name in names)
		{
			var value = (T)Enum.Parse(enumType, name, true);
			result[i] = value;
			i++;
		}
		return result;
	}

	public static Dictionary<T, int> EnumToIndexDict<T>() where T : Enum
	{
		var enumType = typeof(T);
		var names = Enum.GetNames(enumType).ToList();
		Dictionary<T, int> result = [];
		var i = 0;
		foreach (string name in names)
		{
			var value = (T)Enum.Parse(enumType, name, true);
			result.Add(value, i);
			i++;
		}
		return result;
	}
}
