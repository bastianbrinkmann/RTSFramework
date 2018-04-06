using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface ITestResultListener<TTc> where TTc : ITestCase
	{
		void NotifyTestResult(ITestCaseResult<TTc> result);
	}
}