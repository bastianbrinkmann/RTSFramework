using Unity;

namespace RTSFramework.GUI.DependencyInjection
{
	internal static class GUIInitializer
	{
		internal static void InitializeGUI(IUnityContainer container)
		{
			InitializeViewModels(container);
			InitializeViews(container);
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