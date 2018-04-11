using RTSFramework.Contracts;

namespace RTSFramework.Concrete.Reporting
{
	public class FileProcessingResult : ITestProcessingResult
	{
		public string FilePath { get; set; }
	}
}