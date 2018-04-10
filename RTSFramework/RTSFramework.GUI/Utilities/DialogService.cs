using System.Windows;
using RTSFramework.ViewModels.Utilities;

namespace RTSFramework.GUI.Utilities
{
	public class DialogService : IDialogService
	{
		public void ShowErrorMessage(string message)
		{
			MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}