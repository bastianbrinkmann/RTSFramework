using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using RTSFramework.ViewModels;

namespace RTSFramework.GUI.Converters
{
	public class CommitComboboxItemConverter : MarkupExtension, IValueConverter
	{
		private static CommitComboboxItemConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return converter ?? (converter = new CommitComboboxItemConverter());
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var originalValue = value as ObservableCollection<CommitViewModel>;
			return originalValue?.Select(x => $"{WithMaxLength(x.Identifier, 5)} ... - {WithNewLine(WithMaxLength(x.Message, 50))} by {x.Committer ?? ""}");
		}

		private string WithNewLine(string value)
		{
			if (!value.EndsWith("\n"))
			{
				value = value + "\n";
			}

			return value;
		}

		private string WithMaxLength(string value, int maxLength)
		{
			return value?.Substring(0, Math.Min(value.Length, maxLength)) ?? "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}