using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum ProcessingType
    {
		[Description("MSTest")]
        MSTestExecution,
		[Description("MSTest + Create Correspondence")]
		MSTestExecutionCreateCorrespondenceModel,
		[Description("MSTest + Limited Time")]
		MSTestExecutionLimitedTime,
		[Description("CSV Impacted Tests")]
		CsvReporting,
		[Description("GUI Impacted Tests")]
		ListReporting
	}
}