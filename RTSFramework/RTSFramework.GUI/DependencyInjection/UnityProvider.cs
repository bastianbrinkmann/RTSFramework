using RTSFramework.ViewModels;
using Unity;

namespace RTSFramework.GUI.DependencyInjection
{
    internal static class UnityProvider
    {
        private static IUnityContainer UnityContainer { get; } = new UnityContainer();

        public static void Initialize()
        {
            UnityModelInitializer.InitializeModelClasses(UnityContainer);
			GUIInitializer.InitializeGUIClasses(UnityContainer);
        }

		internal static MainWindow GetMainWindow()
	    {
		    return UnityContainer.Resolve<MainWindow>();
	    }

    }
}