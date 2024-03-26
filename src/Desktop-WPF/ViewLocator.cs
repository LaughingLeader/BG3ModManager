﻿using ModManager.Controls;
using ModManager.Models.View;
using ModManager.ViewModels;
using ModManager.ViewModels.Main;
using ModManager.Views;
using ModManager.Views.Main;
using ModManager.Views.StatsValidator;
using ModManager.Windows;

using ReactiveUI;

using Splat;

using System.Windows.Controls;

namespace ModManager;

public class ViewLocatorErrorView : TextBlock, IViewFor
{
	public object ViewModel { get; set; }
}


public class ViewLocator : IViewLocator
{
	private static readonly Type _viewForType = typeof(IViewFor<>);

	public IViewFor ResolveView<T>(T viewModel, string contract = null)
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
		return new ViewLocatorErrorView() { Text = $"Failed to find view for {viewModel}" };
	}

	private static void RegisterConstant<TViewModel, TView>(IMutableDependencyResolver resolver) where TViewModel : ReactiveObject where TView : IViewFor<TViewModel>
	{
		resolver.RegisterLazySingleton<IViewFor<TViewModel>>(() => AppServices.Get<TView>());
	}

	private static void Register<TViewModel, TView>(IMutableDependencyResolver resolver) where TViewModel : ReactiveObject where TView : IViewFor<TViewModel>, new()
	{
		resolver.Register(() => new TView());
	}

	static ViewLocator()
	{
		var resolver = Locator.CurrentMutable;

		RegisterConstant<DeleteFilesViewModel, DeleteFilesConfirmationView>(resolver);
		RegisterConstant<ModOrderViewModel, ModOrderView>(resolver);
		RegisterConstant<ModUpdatesViewModel, ModUpdatesLayout>(resolver);

		Register<DownloadActivityBarViewModel, DownloadActivityBar>(resolver);
		Register<StatsValidatorFileResults, StatsValidatorFileEntryView>(resolver);
		Register<StatsValidatorErrorEntry, StatsValidatorEntryView>(resolver);
		Register<StatsValidatorLineText, StatsValidatorLineView>(resolver);
	}
}