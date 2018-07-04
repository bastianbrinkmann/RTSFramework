using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
	public class VsTestResultAdapter : IArtefactAdapter<VsTestResultToConvert, ITestCaseResult<MSTestTestcase>>
	{
		public ITestCaseResult<MSTestTestcase> Parse(VsTestResultToConvert artefact)
		{
			TestExecutionOutcome outcome;
			switch (artefact.Result.Outcome)
			{
				case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed:
					outcome = TestExecutionOutcome.Passed;
					break;
				case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed:
					outcome = TestExecutionOutcome.Failed;
					break;
				default:
					outcome = TestExecutionOutcome.NotExecuted;
					break;
			}

			return new MSTestTestResult
			{
				TestCase = artefact.MSTestTestcase,
				Outcome = outcome,
				StartTime = artefact.Result.StartTime,
				EndTime = artefact.Result.EndTime,
				ErrorMessage = artefact.Result.ErrorMessage,
				StackTrace = artefact.Result.ErrorStackTrace,
				DurationInSeconds = artefact.Result.Duration.TotalSeconds,
				DisplayName = artefact.Result.DisplayName
			};
		}

		public VsTestResultToConvert Unparse(ITestCaseResult<MSTestTestcase> model, VsTestResultToConvert artefact = null)
		{
			throw new System.NotImplementedException();
		}
	}
}