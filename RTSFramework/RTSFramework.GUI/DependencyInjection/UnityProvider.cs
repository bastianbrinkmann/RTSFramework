using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;
using RTSFramework.GUI.Utilities;
using RTSFramework.ViewModels;
using RTSFramework.ViewModels.Adapter;
using RTSFramework.ViewModels.RunConfigurations;
using Unity;
using Unity.Lifetime;

namespace RTSFramework.GUI.DependencyInjection
{
	public static class UnityProvider
	{
		private static IUnityContainer UnityContainer { get; } = new UnityContainer();

		public static void Initialize()
		{
			UnityModelInitializer.InitializeModelClasses(UnityContainer);
			UnityGUIInitializer.InitializeGUIClasses(UnityContainer);

			UnityContainer.RegisterType<IApplicationClosedHandler, ApplicationClosedHandler>(new ContainerControlledLifetimeManager());
			
			UnityContainer.RegisterType<ISettingsProvider, SettingsProvider>(new ContainerControlledLifetimeManager());
			UnityContainer.RegisterType<UserSettingsProvider>(new ContainerControlledLifetimeManager());
			UnityContainer.RegisterType<IArtefactAdapter<string, UserSettings>, JsonFileUserSettingsAdapter>(new ContainerControlledLifetimeManager());
			UnityContainer.RegisterType<ILoggingHelper, LoggingHelper>(new ContainerControlledLifetimeManager());

			var applicationClosedHandler = UnityContainer.Resolve<IApplicationClosedHandler>();
			var userSettingsProvider = UnityContainer.Resolve<UserSettingsProvider>();

			applicationClosedHandler.AddApplicationClosedListener(userSettingsProvider);
		}

		public static MainWindow GetMainWindow()
		{
			return UnityContainer.Resolve<MainWindow>();
		}

		public static IApplicationClosedHandler GetApplicationClosedHandler()
		{
			return UnityContainer.Resolve<IApplicationClosedHandler>();
		}
	}
}