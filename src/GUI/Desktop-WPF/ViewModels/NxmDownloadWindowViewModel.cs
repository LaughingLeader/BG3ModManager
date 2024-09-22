﻿using System.Windows.Input;

namespace ModManager.ViewModels;

public class NxmDownloadWindowViewModel : BaseWindowViewModel
{
	[Reactive] public string Url { get; set; }

	public ICommand DownloadCommand { get; }

	public NxmDownloadWindowViewModel()
	{
		var canConfirm = this.WhenAnyValue(x => x.Url).Select(x => !String.IsNullOrEmpty(x) && x.StartsWith("nxm://"));
		DownloadCommand = ReactiveCommand.Create(() =>
		{
			AppServices.NexusMods.ProcessNXMLinkBackground(Url);
		}, canConfirm);
	}
}