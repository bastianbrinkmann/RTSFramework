namespace RTSFramework.ViewModels.Utilities
{
	public interface IDialogService
	{
		void ShowError(string message, string title = "Error");

		void ShowWarning(string message, string title = "Warning");

		void ShowInformation(string message, string title = "Information");
	}
}