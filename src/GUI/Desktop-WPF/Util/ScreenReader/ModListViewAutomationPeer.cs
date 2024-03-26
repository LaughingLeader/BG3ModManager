using ModManager.Controls.Views;

using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace ModManager.Util.ScreenReader;

public class ModListViewAutomationPeer : CachedAutomationPeer
{
	private readonly ModListView _listView;

	public ModListViewAutomationPeer(ModListView owner) : base(owner)
	{
		_listView = owner;
	}

	protected override string GetNameCore()
	{
		return Owner.GetValue(AutomationProperties.NameProperty) as string ?? string.Empty;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.List;
	}

	override public bool HasNullChildElement()
	{
		foreach (var c in _listView.Items)
		{
			if (c == null)
			{
				DivinityApp.Log("Found a null entry in ModListViewAutomationPeer");
				return true;
			}
		}
		return false;
	}
}
