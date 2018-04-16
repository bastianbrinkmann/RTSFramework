using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Contracts.Models
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum TestExecutionOutcome
    {
		[Description("")]
		None,
		[Description("Passed")]
		Passed,
		[Description("Failed")]
		Failed,
		[Description("Not Executed")]
		NotExecuted
    }
}