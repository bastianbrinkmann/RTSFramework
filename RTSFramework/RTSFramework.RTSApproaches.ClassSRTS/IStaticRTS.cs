using System;
using System.Collections.Generic;
using System.Threading;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	public interface IStaticRTS<TModel, TDelta, TTestCase, TDataStructure> where TTestCase : ITestCase where TDelta : IDelta<TModel> where TModel : IProgramModel
	{
		ISet<TTestCase> SelectTests(TDataStructure dataStructure, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TDelta delta, CancellationToken cancellationToken);

		ICorrespondenceModel CorrespondenceModel { get; }
	}
}