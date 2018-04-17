using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using RTSFramework.Contracts.Models;

namespace RTSFramework.GUI.Converters
{
	public class TestExecutionOutcomeImageConverter : MarkupExtension, IValueConverter
	{
		private static TestExecutionOutcomeImageConverter testExecutionOutcomeImageConverter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (testExecutionOutcomeImageConverter == null)
			{
				testExecutionOutcomeImageConverter = new TestExecutionOutcomeImageConverter();
			}

			return testExecutionOutcomeImageConverter;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
			{
				return null;
			}

			var testExecutionOutcome = (TestExecutionOutcome) value;

			const string resourcesFolder = @"Resources\";

			switch (testExecutionOutcome)
			{
				case TestExecutionOutcome.Passed:
					return resourcesFolder + "passed.png";
				case TestExecutionOutcome.Failed:
					return resourcesFolder + "failed.png";
				case TestExecutionOutcome.NotExecuted:
					return resourcesFolder + "other.png";
				default:
					return null;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}