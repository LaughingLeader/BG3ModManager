﻿using DivinityModManager.Controls;

using System.Windows.Automation.Peers;

namespace DivinityModManager.Util.ScreenReader;

public class AlertBarAutomationPeer : FrameworkElementAutomationPeer
{
	private readonly AlertBar alertBar;

	public AlertBarAutomationPeer(AlertBar owner) : base(owner)
	{
		alertBar = owner;
	}
	protected override string GetNameCore()
	{
		return alertBar.GetText();
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.StatusBar;
	}

	protected override List<AutomationPeer> GetChildrenCore()
	{
		List<AutomationPeer> peers = new();
		var textElements = alertBar.GetTextElements();
		if (textElements.Count > 0)
		{
			foreach (var element in textElements)
			{
				var peer = UIElementAutomationPeer.CreatePeerForElement(element);
				if (peer != null)
				{
					peers.Add(peer);
				}
			}
		}
		return peers;
	}
}
