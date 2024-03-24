using ReactiveUI;

using System.ComponentModel;
using System.Reflection;

namespace ModManager.Extensions;

public static class ModelExtensions
{
	public static void SetToDefault(this ReactiveObject model)
	{
		/*PropertyInfo[] props = model.GetType().GetProperties();
		foreach (PropertyInfo prop in props)
		{
			var d = prop.GetCustomAttribute<DefaultValueAttribute>();
			if (d != null && prop.GetValue(model) != d.Value)
			{
				prop.SetValue(model, d.Value);
			}
		}*/
		var props = TypeDescriptor.GetProperties(model.GetType());
		foreach (PropertyDescriptor pr in props)
		{
			if (pr.CanResetValue(model))
			{
				pr.ResetValue(model);
			}
		}
	}

	public static void SetFrom<T>(this T target, T from) where T : ReactiveObject
	{
		var props = TypeDescriptor.GetProperties(target.GetType());
		foreach (PropertyDescriptor pr in props)
		{
			var value = pr.GetValue(from);
			if (value != null)
			{
				pr.SetValue(target, value);
			}
		}
	}

	public static void SetFrom<T, T2>(this T target, T from) where T : ReactiveObject where T2 : Attribute
	{
		var attributeType = typeof(T2);
		var props = typeof(T).GetRuntimeProperties().Where(prop => Attribute.IsDefined(prop, attributeType)).ToList();
		foreach (var pr in props)
		{
			var value = pr.GetValue(from);
			if (value != null)
			{
				pr.SetValue(target, value);
			}
		}
	}
}
