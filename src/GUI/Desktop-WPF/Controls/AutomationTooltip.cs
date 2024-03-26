using ModManager.Util;

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace ModManager.Controls;

public class AutomationTooltip : ToolTip
{
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new AutomationTooltipPeer(this);
	}
}
