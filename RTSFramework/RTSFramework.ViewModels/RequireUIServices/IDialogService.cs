namespace RTSFramework.ViewModels.RequireUIServices
{
	public interface IDialogService
	{
		void ShowError(string message, string title = "Error");

		void ShowWarning(string message, string title = "Warning");

		void ShowInformation(string message, string title = "Information");

		bool ShowQuestion(string message, string title = "Question");
	}
}