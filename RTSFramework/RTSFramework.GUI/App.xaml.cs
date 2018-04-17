using System;
using System.Windows;
using RTSFramework.GUI.DependencyInjection;

namespace RTSFramework.GUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			var splashScreen = new SplashScreen(@"Resources\loading.png");
			splashScreen.Show(false, true);

			base.OnStartup(e);

			UnityProvider.Initialize();
			var mainWindow = UnityProvider.GetMainWindow();

			splashScreen.Close(TimeSpan.FromMilliseconds(750));
			mainWindow.Show();
		}
	}
}
