using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace RTSFramework.GUI.Converters
{
	public class InverseBooleanConverter : MarkupExtension, IValueConverter
	{
		private static InverseBooleanConverter inverseBooleanConverter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (inverseBooleanConverter == null)
			{
				inverseBooleanConverter = new InverseBooleanConverter();
			}

			return inverseBooleanConverter;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool originalValue = value != null && (bool)value;
			return !originalValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool originalValue = value != null && (bool)value;
			return !originalValue;
		}
	}
}