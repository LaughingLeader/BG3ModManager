using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;

namespace ModManager.Util.ScreenReader;

public class CachedAutomationPeer : FrameworkElementAutomationPeer
{
	public CachedAutomationPeer(FrameworkElement owner) : base(owner) { }

	private List<AutomationPeer> _cachedAutomationPeers;

	private static AutomationPeer CreatePeerForElementSafe(UIElement element)
	{
		try
		{
			return FrameworkElementAutomationPeer.CreatePeerForElement(element);
		}
		catch (Exception)
		{
			return null;
		}
	}

	internal static List<AutomationPeer> GetChildrenRecursively(UIElement uiElement)
	{
		List<AutomationPeer> children = [];
		var childrenCount = VisualTreeHelper.GetChildrenCount(uiElement);

		for (var child = 0; child < childrenCount; child++)
		{
			if (!(VisualTreeHelper.GetChild(uiElement, child) is UIElement element))
				continue;

			var peer = CreatePeerForElementSafe(element);
			if (peer != null)
				children.Add(peer);
			else
			{
				var returnedChildren = GetChildrenRecursively(element);
				if (returnedChildren != null)
					children.AddRange(returnedChildren);
			}
		}

		if (children.Count == 0)
			return null;

		return children;
	}

	public virtual bool HasNullChildElement()
	{
		foreach (var c in this.Owner.FindVisualChildren<UIElement>())
		{
			if (c == null)
			{
				return true;
			}
		}
		return false;
	}

	public virtual List<AutomationPeer> GetPeersFromElements()
	{
		return GetChildrenRecursively(Owner);
	}

	protected override List<AutomationPeer> GetChildrenCore()
	{
		if (HasNullChildElement())
		{
			return _cachedAutomationPeers;
		}
		else
		{
			_cachedAutomationPeers = GetPeersFromElements();
		}
		return _cachedAutomationPeers;
	}
}
