using ModManager.Windows;

using System.Windows.Automation.Peers;

namespace ModManager.Util.ScreenReader;

public class MainWindowAutomationPeer : CachedAutomationPeer
{
	private readonly MainWindow mainWindow;
	public MainWindowAutomationPeer(MainWindow owner) : base(owner)
	{
		mainWindow = owner;
	}

	protected override string GetNameCore()
	{
		if (mainWindow.ViewModel != null)
		{
			return mainWindow.ViewModel.Title;
		}
		else
		{
			return "Divinity Mod Manager";
		}
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Window;
	}
}
