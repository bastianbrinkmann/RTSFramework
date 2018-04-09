using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Contracts.DeltaDiscoverer
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum DiscoveryType
    {
		[Description("Local Discovery")]
        LocalDiscovery,
		[Description("Intended Changes")]
		UserIntendedChangesDiscovery
    }
}