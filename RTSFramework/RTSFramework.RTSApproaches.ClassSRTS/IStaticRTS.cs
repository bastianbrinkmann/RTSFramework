using System.Collections.Generic;
using System.Threading;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.RTSApproaches.Static
{
	public interface IStaticRTS<TProgram, TDelta, TTestCase, TDataStructure> where TTestCase : ITestCase where TDelta : IDelta<TProgram> where TProgram : IProgramModel
	{
		ISet<TTestCase> SelectTests(TDataStructure dataStructure, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TDelta programDelta, CancellationToken cancellationToken);

		ICorrespondenceModel CorrespondenceModel { get; }
	}
}