using System.Threading;
using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.Core.Contracts
{
	public interface IDataStructureProvider<TDataStructure, TModel> where TModel : IProgramModel
	{
		TDataStructure GetDataStructureForProgram(TModel model, CancellationToken cancellationToken);
		void PersistDataStructure(TDataStructure dataStructure);
	}
}