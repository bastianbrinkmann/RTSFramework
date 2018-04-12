using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.ViewModels;
using RTSFramework.ViewModels.RunConfigurations;
using Unity;

namespace RTSFramework.GUI.DependencyInjection
{
    public static class UnityProvider
    {
        private static IUnityContainer UnityContainer { get; } = new UnityContainer();

        public static void Initialize()
        {
            UnityModelInitializer.InitializeModelClasses(UnityContainer);
			UnityGUIInitializer.InitializeGUIClasses(UnityContainer);
		}

		public static MainWindow GetMainWindow()
	    {
		    return UnityContainer.Resolve<MainWindow>();
	    }
    }
}