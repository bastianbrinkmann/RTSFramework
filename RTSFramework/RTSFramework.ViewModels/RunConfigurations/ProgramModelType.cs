using System.ComponentModel;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.ViewModels.RunConfigurations
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum ProgramModelType
	{
		[Description("Git Model")]
		GitModel,
		[Description("TFS 2010 Model")]
		TFS2010Model
	}
}