using System.Windows;
using RTSFramework.ViewModels;

namespace RTSFramework.GUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow(MainWindowViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}
	}
}
