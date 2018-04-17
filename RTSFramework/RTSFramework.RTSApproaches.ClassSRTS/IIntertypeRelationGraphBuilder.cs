using System.Threading;
using RTSFramework.Concrete.CSharp.Core.Models;

namespace RTSFramework.RTSApproaches.ClassSRTS
{
	public interface IIntertypeRelationGraphBuilder
	{
		IntertypeRelationGraph BuildIntertypeRelationGraph(CSharpProgramModel sourceModel, CancellationToken cancellationToken);
	}
}