using System.Windows;
using RTSFramework.ViewModels.RequireUIServices;

namespace RTSFramework.GUI.RequireUIServices
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

		public bool ShowQuestion(string message, string title = "Question")
		{
			var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

			return result == MessageBoxResult.Yes;
		}
	}
}