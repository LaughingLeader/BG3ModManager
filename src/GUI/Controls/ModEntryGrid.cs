using ModManager.Util.ScreenReader;

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace ModManager.Controls;

public class ModEntryGrid : Grid
{
	public ModEntryGrid() : base() { }

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ModEntryGridAutomationPeer(this);
	}
}
