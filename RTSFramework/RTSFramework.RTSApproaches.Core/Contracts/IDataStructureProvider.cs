using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.Core.Contracts
{
	public interface IDataStructureProvider<TDataStructure, TModel> where TModel : IProgramModel
	{
		Task<TDataStructure> GetDataStructure(TModel model, CancellationToken cancellationToken);
		Task PersistDataStructure(TDataStructure dataStructure);
	}
}