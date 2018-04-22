using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Shell;

namespace RTSFramework.GUI.Converters
{
	public class ProgressStateConverter : MarkupExtension, IValueConverter
	{
		private static ProgressStateConverter progressStateConverter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return progressStateConverter ?? (progressStateConverter = new ProgressStateConverter());
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool isRunning = value != null && (bool)value;
			return isRunning ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.None;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}