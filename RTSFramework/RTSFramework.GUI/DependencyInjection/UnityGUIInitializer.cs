using RTSFramework.GUI.RequireUIServices;
using RTSFramework.GUI.Utilities;
using RTSFramework.ViewModels;
using RTSFramework.ViewModels.RequireUIServices;
using Unity;

namespace RTSFramework.GUI.DependencyInjection
{
	internal static class UnityGUIInitializer
	{
		internal static void InitializeGUIClasses(IUnityContainer container)
		{
			InitializeUtilities(container);
			InitializeViewModels(container);
			InitializeViews(container);
		}

		private static void InitializeUtilities(IUnityContainer container)
		{
			container.RegisterType<IDialogService, DialogService>();
		}

		private static void InitializeViewModels(IUnityContainer container)
		{
			container.RegisterType<MainWindowViewModel>();
		}

		private static void InitializeViews(IUnityContainer container)
		{
			container.RegisterType<MainWindow>();
		}
	}
}