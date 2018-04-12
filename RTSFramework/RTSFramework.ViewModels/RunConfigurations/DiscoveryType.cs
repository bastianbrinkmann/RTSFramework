using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum DiscoveryType
    {
		[Description("Between Commits")]
		VersionCompare,
		[Description("Uncommitted Changes")]
        LocalDiscovery,
		[Description("Intended Changes")]
		UserIntendedChangesDiscovery,
	}
}