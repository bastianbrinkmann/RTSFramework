using System.Threading;
using System.Threading.Tasks;

namespace RTSFramework.Contracts.Adapter
{
	public abstract class CancelableArtefactAdapter<TArtefact, TModel> : IArtefactAdapter<TArtefact, TModel>
	{
		public abstract Task<TModel> Parse(TArtefact artefact, CancellationToken cancellationToken);

		public abstract  Task Unparse(TModel model, TArtefact artefact, CancellationToken cancellationToken);
		public TModel Parse(TArtefact artefact)
		{
			return Parse(artefact, default(CancellationToken)).Result;
		}

		public void Unparse(TModel model, TArtefact artefact)
		{
			Unparse(model, artefact, default(CancellationToken));
		}
	}
}