using Microsoft.Msagl.Drawing;
using Prism.Mvvm;

namespace RTSFramework.ViewModels.RequireUIServices
{
	public interface IDialogService
	{
		void ShowError(string message, string title = "Error");

		void ShowWarning(string message, string title = "Warning");

		void ShowInformation(string message, string title = "Information");

		bool ShowQuestion(string message, string title = "Question");

		bool SelectFile(string initialDirectory, string fileExtensionPattern, out string selectedPath);

		bool SelectDirectory(string initialDirectory, out string selectedDirectory);

		T OpenDialogByViewModel<T>() where T : BindableBase;
		void ShowGraph(Graph graph);
	}
}