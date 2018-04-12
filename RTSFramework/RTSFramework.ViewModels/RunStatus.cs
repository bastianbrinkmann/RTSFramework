using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum RunStatus
	{
		[Description("Ready")]
		Ready,
		[Description("Running...")]
		Running,
		[Description("Completed!")]
		Completed,
		[Description("Cancelled!")]
		Cancelled,
		[Description("Failed!")]
		Failed
	}
}