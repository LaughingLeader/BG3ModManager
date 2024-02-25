﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DivinityModManager
{
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
	}
}
