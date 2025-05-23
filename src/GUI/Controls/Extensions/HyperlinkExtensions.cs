﻿using DivinityModManager.Util;

using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace DivinityModManager.Controls.Extensions;

/// <summary>
/// Source: https://stackoverflow.com/a/11433814
/// </summary>
public static class HyperlinkExtensions
{
	public static bool GetIsExternal(DependencyObject obj)
	{
		return (bool)obj.GetValue(IsExternalProperty);
	}

	public static void SetIsExternal(DependencyObject obj, bool value)
	{
		obj.SetValue(IsExternalProperty, value);
	}
	public static readonly DependencyProperty IsExternalProperty =
		DependencyProperty.RegisterAttached("IsExternal", typeof(bool), typeof(HyperlinkExtensions), new UIPropertyMetadata(false, OnIsExternalChanged));

	private static void OnIsExternalChanged(object sender, DependencyPropertyChangedEventArgs args)
	{
		var hyperlink = sender as Hyperlink;

		if ((bool)args.NewValue)
			hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
		else
			hyperlink.RequestNavigate -= Hyperlink_RequestNavigate;
	}

	private static void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
	{
		ProcessHelper.TryOpenUrl(e.Uri.AbsoluteUri);
		e.Handled = true;
	}
}
