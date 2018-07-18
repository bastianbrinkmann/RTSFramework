using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum ProgramLocation
	{
		[Description("Git Repository")]
		GitRepository,
		[Description("Local Program")]
		LocalProgram
	}
}