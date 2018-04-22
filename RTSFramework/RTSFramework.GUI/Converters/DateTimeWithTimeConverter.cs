using System;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Shell;

namespace RTSFramework.GUI.Converters
{
	public class DateTimeWithTimeConverter : MarkupExtension, IValueConverter
	{
		private static DateTimeWithTimeConverter progressStateConverter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return progressStateConverter ?? (progressStateConverter = new DateTimeWithTimeConverter());
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			DateTimeOffset time = (DateTimeOffset)value;

			return time.ToString("HH:mm:ss:fff");
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}