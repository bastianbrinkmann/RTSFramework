using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum RTSApproachType
    {
		[Description("Retest All")]
		RetestAll,
		[Description("Dynamic RTS")]
		DynamicRTS,
		[Description("ClassSRTS")]
		ClassSRTS
    }
}