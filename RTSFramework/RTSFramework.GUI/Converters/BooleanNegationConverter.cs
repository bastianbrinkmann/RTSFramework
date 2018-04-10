using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace RTSFramework.GUI.Converters
{
	public class BooleanNegationConverter : MarkupExtension, IValueConverter
	{
		private static BooleanNegationConverter booleanNegationConverter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (booleanNegationConverter == null)
			{
				booleanNegationConverter = new BooleanNegationConverter();
			}

			return booleanNegationConverter;
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