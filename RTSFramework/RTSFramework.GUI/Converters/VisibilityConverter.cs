using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RTSFramework.GUI.Converters
{
	public class VisibilityConverter : MarkupExtension, IValueConverter
	{
		private static VisibilityConverter visibilityConverter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (visibilityConverter == null)
			{
				visibilityConverter = new VisibilityConverter();
			}

			return visibilityConverter;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool isVisible = value != null && (bool)value;
			return isVisible ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}