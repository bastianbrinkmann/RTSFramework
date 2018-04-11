using System.Windows;
using RTSFramework.ViewModels.Utilities;

namespace RTSFramework.GUI.Utilities
{
	public class DialogService : IDialogService
	{
		public void ShowError(string message, string title = "Error")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		public void ShowWarning(string message, string title = "Warning")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		public void ShowInformation(string message, string title = "Information")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}