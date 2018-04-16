using System;
using System.Collections.Generic;
using System.Windows;
using RTSFramework.GUI.RequireUIServices;
using RTSFramework.ViewModels;
using RTSFramework.ViewModels.RequireUIServices;
using Unity;
using Unity.Injection;
using Unity.Lifetime;

namespace RTSFramework.GUI.DependencyInjection
{
	internal static class UnityGUIInitializer
	{
		private static readonly Dictionary<Type, Type> ViewModelsViewsDictionary = new Dictionary<Type, Type>
		{
			{typeof(MainWindowViewModel), typeof(MainWindow) },
			{typeof(IntendedChangesDialogViewModel), typeof(IntendedChangesDialog) }
		};

		internal static void InitializeGUIClasses(IUnityContainer container)
		{
			InitializeUtilities(container);
			InitializeViewModels(container);
			InitializeViews(container);

			InitializeViewModelsViewsFactory(container);
		}

		private static void InitializeUtilities(IUnityContainer container)
		{
			container.RegisterType<IDialogService, DialogService>(new ContainerControlledLifetimeManager());
			container.RegisterType<IApplicationUiExecutor, ApplicationUiExecutor>(new ContainerControlledLifetimeManager());
		}

		private static void InitializeViewModelsViewsFactory(IUnityContainer container)
		{
			container.RegisterType<Func<Type, Window>>(
				new InjectionFactory(c =>
				new Func<Type, Window> (viewModelType =>
					{
						var viewType = ViewModelsViewsDictionary[viewModelType];
						
						var view = (Window)container.Resolve(viewType);
						view.DataContext = container.Resolve(viewModelType);

						return view;
					})));

		}

		private static void InitializeViewModels(IUnityContainer container)
		{
			container.RegisterType<MainWindowViewModel>();
			container.RegisterType<IntendedChangesDialogViewModel>();
		}

		private static void InitializeViews(IUnityContainer container)
		{
			container.RegisterType<MainWindow>();
			container.RegisterType<IntendedChangesDialog>();
		}
	}
}