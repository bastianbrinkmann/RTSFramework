using System;
using System.Windows;
using System.Windows.Forms;
using Prism.Mvvm;
using RTSFramework.ViewModels.RequireUIServices;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RTSFramework.GUI.RequireUIServices
{
	public class DialogService : IDialogService
	{
		private readonly Func<Type, Window> viewModelViewsFactory;

		public DialogService(Func<Type, Window> viewModelViewsFactory)
		{
			this.viewModelViewsFactory = viewModelViewsFactory;
		}

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

		public bool SelectFile(string initialDirectory, string fileExtensionPattern, out string selectedPath)
		{
			var openFileDialog = new OpenFileDialog();

			if (fileExtensionPattern != null)
			{
				openFileDialog.Filter = fileExtensionPattern;
			}

			if (initialDirectory != null)
			{
				openFileDialog.InitialDirectory = initialDirectory;
			}

			if (openFileDialog.ShowDialog() == true)
			{
				selectedPath = openFileDialog.FileName;
				return true;
			}

			selectedPath = null;
			return false;
		}

		public bool SelectDirectory(string initialDirectory, out string selectedDirectory)
		{
			var openDirectoryDialog = new FolderBrowserDialog();

			if (initialDirectory != null)
			{
				openDirectoryDialog.SelectedPath = initialDirectory;
			}

			if(openDirectoryDialog.ShowDialog() == DialogResult.OK)
			{
				selectedDirectory = openDirectoryDialog.SelectedPath;
				return true;
			}

			selectedDirectory = null;
			return false;
		}

		public T OpenDialogByViewModel<T>() where T : BindableBase
		{
			var view = viewModelViewsFactory(typeof(T));
			view.Show();

			return (T) view.DataContext;
		}
	}
}