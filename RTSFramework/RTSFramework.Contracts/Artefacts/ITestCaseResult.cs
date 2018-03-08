namespace RTSFramework.Contracts.Artefacts
{
	public interface ITestCaseResult<TTC> where TTC : ITestCase
	{
		TTC AssociatedTestCase { get; }

		bool Successful { get; }
	}
}