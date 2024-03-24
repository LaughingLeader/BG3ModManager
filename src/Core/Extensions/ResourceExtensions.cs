using LSLib.LS;

namespace ModManager.Extensions;

static public class ResourceExtensions
{

	public static Node FindNode(this Node node, string name)
	{
		if (node.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
		{
			return node;
		}
		else
		{
			return FindNode(node.Children, name);
		}
	}

	public static Node FindNode(this Dictionary<string, List<Node>> children, string name)
	{
		foreach (var kvp in children)
		{
			if (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				return kvp.Value.FirstOrDefault();
			}

			foreach (var node in kvp.Value)
			{
				var match = FindNode(node, name);
				if (match != null)
				{
					return match;
				}
			}
		}
		return null;
	}

	public static Node FindNode(this Region region, string name)
	{
		foreach (var kvp in region.Children)
		{
			if (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				return kvp.Value.First();
			}
		}

		var match = FindNode(region.Children, name);
		if (match != null)
		{
			return match;
		}

		return null;
	}

	public static Node FindNode(this Resource resource, string name)
	{
		foreach (var region in resource.Regions.Values)
		{
			var match = FindNode(region, name);
			if (match != null)
			{
				return match;
			}
		}

		return null;
	}

	public static bool TryFindNode(this Resource resource, string name, out Node targetNode)
	{
		targetNode = FindNode(resource, name);
		return targetNode != null;
	}

	public static bool TryFindNode(this Region region, string name, out Node targetNode)
	{
		targetNode = FindNode(region, name);
		return targetNode != null;
	}

	public static Region FindRegion(this Resource resource, string name)
	{
		foreach (var region in resource.Regions.Values)
		{
			if (region.RegionName == name)
			{
				return region;
			}
		}

		return null;
	}

	public static bool TryFindRegion(this Resource resource, string name, out Region targetRegion)
	{
		foreach (var region in resource.Regions.Values)
		{
			if (region.RegionName == name)
			{
				targetRegion = region;
				return true;
			}
		}
		targetRegion = null;
		return false;
	}

	private static void PrintDebugNode(Node node, string indent = "", int index = -1)
	{
		if (index > -1)
		{
			DivinityApp.Log($"{indent} [{index}] Node: Name({node.Name}) Children{node.ChildCount}");
		}
		else
		{
			DivinityApp.Log($"{indent} Node: Name({node.Name}) Children{node.ChildCount}");
		}

		DivinityApp.Log($"{indent}Attributes ({node.Attributes.Count})");
		if (node.Attributes.Count > 0)
		{
			foreach (var entry in node.Attributes)
			{
				DivinityApp.Log($"{indent}  Attribute: Name({entry.Key}) Value({entry.Value.Value}) Type({entry.Value.Type})");
			}
		}

		DivinityApp.Log($"{indent}Children ({node.ChildCount})");
		if (node.ChildCount > 0)
		{
			foreach (var entry in node.Children)
			{
				DivinityApp.Log($"{indent}  Child List({entry.Key})");
				int i = 0;
				foreach (var node2 in entry.Value)
				{
					PrintDebugNode(node2, indent + " ", i);
					i++;
				}
			}
		}
	}

	public static void PrintDebug(this Resource resource)
	{
		foreach (var kvpRegion in resource.Regions)
		{
			DivinityApp.Log($"Region: Key({kvpRegion.Key}) Name({kvpRegion.Value.Name}) RegionName({kvpRegion.Value.RegionName})");
			foreach (var nodeList in kvpRegion.Value.Children)
			{
				DivinityApp.Log($" Children: Key({nodeList.Key})");
				for (var i = 0; i < nodeList.Value.Count; i++)
				{
					var node = nodeList.Value[i];
					PrintDebugNode(node, "  ", i);
				}
			}
		}
	}
}
