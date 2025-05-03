namespace ModManager.Extensions;

public static class DictionaryExtensions
{
	public static object FindKeyValue(this Dictionary<string, object> dict, string key, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
	{
		foreach (var kvp in dict)
		{
			if (kvp.Key.Equals(key, stringComparison))
			{
				return kvp.Value;
			}
			else if (kvp.Value.GetType() == typeof(Dictionary<string, object>))
			{
				var subDict = (Dictionary<string, object>)kvp.Value;
				var val = subDict.FindKeyValue(key, stringComparison);
				if (val != null) return val;
			}
		}
		return null;
	}

	private static object FindKeyValue_Recursive(object obj, string key, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
	{
		if (obj.GetType() == typeof(Dictionary<string, object>))
		{
			var subDict = (Dictionary<string, object>)obj;
			var val = subDict.FindKeyValue(key, stringComparison);
			if (val != null) return val;
		}
		else if (obj is IList list)
		{
			foreach (var childobj in list)
			{
				var val = FindKeyValue_Recursive(childobj, key, stringComparison);
				if (val != null) return val;
			}
		}
		else if (obj is IEnumerable enumerable)
		{
			foreach (var childobj in enumerable)
			{
				var val = FindKeyValue_Recursive(childobj, key, stringComparison);
				if (val != null) return val;
			}
		}
		return null;
	}

	public static bool TryFindKeyValue(this Dictionary<string, object> dict, string key, out object valObj, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
	{
		foreach (var kvp in dict)
		{
			if (kvp.Key.Equals(key, stringComparison))
			{
				valObj = kvp.Value;
				return true;
			}
			else
			{
				var val = FindKeyValue_Recursive(kvp.Value, key, stringComparison);
				if (val != null)
				{
					valObj = val;
					return true;
				}
			}
		}
		valObj = null;
		return false;
	}

	/// <summary>
	/// ToDictionary that allows duplicate key entries.
	/// Source: https://stackoverflow.com/a/22508992/2290477
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector"></param>
	/// <param name="elementSelector"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	public static Dictionary<TKey, TElement> SafeToDictionary<TSource, TKey, TElement>(
	this IEnumerable<TSource> source,
	Func<TSource, TKey?> keySelector,
	Func<TSource, TElement> elementSelector,
	IEqualityComparer<TKey>? comparer = null)
	{
		var dictionary = new Dictionary<TKey, TElement>(comparer);

		if (source == null)
		{
			return dictionary;
		}

		foreach (var element in source)
		{
			var key = keySelector(element);
			if(key != null)
			{
				dictionary[key] = elementSelector(element);
			}
		}

		return dictionary;
	}
}
