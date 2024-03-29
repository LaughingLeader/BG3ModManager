﻿

using Avalonia.Controls;

namespace ModManager;

public class ViewLocatorErrorView : TextBlock, IViewFor
{
	public object? ViewModel { get; set; }
}

public class ViewLocator : IViewLocator
{
	private static readonly Type _viewForType = typeof(IViewFor<>);

	public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
	{
		if(viewModel != null)
		{
			try
			{
				var viewType = _viewForType.MakeGenericType(viewModel.GetType());
				var registered = Locator.Current.GetService(viewType, contract);
				if (registered is IViewFor view)
				{
					return view;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error fetching view: {ex}");
			}
		}
		return new ViewLocatorErrorView() { Text = $"Failed to find view for {viewModel}" };
	}
}