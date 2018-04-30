using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RTSFramework.ViewModels;

namespace RTSFramework.GUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow(MainWindowViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
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
	}
}
