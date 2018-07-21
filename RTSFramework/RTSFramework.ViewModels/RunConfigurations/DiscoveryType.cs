using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum DiscoveryType
    {
		[Description("Offline Discovery")]
		OfflineDiscovery,
		[Description("Intended Changes")]
		UserIntendedChangesDiscovery,
	}
}