using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.Core.Contracts
{
	public interface IDataStructureBuilder<TDataStructure, TModel> where TModel : IProgramModel
	{
		Task<TDataStructure> GetDataStructure(TModel model, CancellationToken cancellationToken);
	}
}