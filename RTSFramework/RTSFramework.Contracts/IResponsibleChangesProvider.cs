using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
	public interface IResponsibleChangesProvider
	{
		IList<string> GetResponsibleChangesForImpactedTest(string testCaseId);
	}
}