using System.ComponentModel;
using System.Windows;
using RTSFramework.ViewModels;

namespace RTSFramework.GUI
{
	/// <summary>
	/// Interaction logic for IntendedChangesDialog.xaml
	/// </summary>
	public partial class IntendedChangesDialog
	{

		private IntendedChangesDialogViewModel viewModel;
		public IntendedChangesDialog(IntendedChangesDialogViewModel viewModel)
		{
			InitializeComponent();
			this.viewModel = viewModel;
			viewModel.PropertyChanged += ViewModelOnPropertyChanged;
			SetFontSize();
		}

		private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName == nameof(MainWindowViewModel.FontSize))
			{
				SetFontSize();
			}
		}

		private void SetFontSize()
		{
			FontSize = viewModel.FontSize;
		}
	}
}
