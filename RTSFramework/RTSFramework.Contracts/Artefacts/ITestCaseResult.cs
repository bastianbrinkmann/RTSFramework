namespace RTSFramework.Contracts.Artefacts
{
	public interface ITestCaseResult<TTC> where TTC : ITestCase
	{
		TTC AssociatedTestCase { get; }

		TestCaseResultType Outcome { get; }
	}
}