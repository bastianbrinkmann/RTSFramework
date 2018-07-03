using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RTSFramework.ViewModels;

namespace RTSFramework.GUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly MainWindowViewModel viewModel;

		public MainWindow(MainWindowViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
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

		private void ShowHideDetails(object sender, RoutedEventArgs e)
		{
			for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
			{
				var gridRow = vis as DataGridRow;
				if (gridRow != null)
				{
					gridRow.DetailsVisibility = gridRow.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					var testResult = gridRow.DataContext as TestResultListViewItemViewModel;
					if (testResult != null)
					{
						testResult.AreChildResultsShown = gridRow.DetailsVisibility == Visibility.Visible;
					}

					break;
				}
			}
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}
	}
}
