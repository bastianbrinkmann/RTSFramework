using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum ProcessingType
    {
		[Description("MSTest")]
        MSTestExecution,
		[Description("MSTest + Coverage")]
		MSTestExecutionWithCoverage,
		[Description("CSV File")]
		CsvReporting,
		[Description("Only Identify")]
		ListReporting
    }
}