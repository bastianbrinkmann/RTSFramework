using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum DiscoveryType
    {
		[Description("Git Discovery")]
		GitDiscovery,
		[Description("Intended Changes")]
		UserIntendedChangesDiscovery,
	}
}