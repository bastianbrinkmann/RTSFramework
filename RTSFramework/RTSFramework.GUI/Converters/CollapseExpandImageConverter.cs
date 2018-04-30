using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace RTSFramework.GUI.Converters
{
	public class CollapseExpandImageConverter : MarkupExtension, IValueConverter
	{
		private static CollapseExpandImageConverter collapseExpandImageConverter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (collapseExpandImageConverter == null)
			{
				collapseExpandImageConverter = new CollapseExpandImageConverter();
			}

			return collapseExpandImageConverter;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool areChildTestcasesShown = value != null && (bool) value;

			const string resourcesFolder = @"Resources\";

			return areChildTestcasesShown ? resourcesFolder + "collapse.png" : resourcesFolder + "expand.png";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}