using System.Windows;
using RTSFramework.GUI.DependencyInjection;

namespace RTSFramework.GUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			UnityProvider.Initialize();
			var mainWindow = UnityProvider.GetMainWindow();
			mainWindow.Show();
		}
	}
}
