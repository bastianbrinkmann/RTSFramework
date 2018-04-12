using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum DiscoveryType
    {
		[Description("Local Discovery")]
        LocalDiscovery,
		[Description("Intended Changes")]
		UserIntendedChangesDiscovery,
	}
}