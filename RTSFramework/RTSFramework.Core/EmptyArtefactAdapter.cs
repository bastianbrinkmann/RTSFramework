using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Core
{
	public class EmptyArtefactAdapter<TModel> : IArtefactAdapter<object, TModel>
	{
		public TModel Parse(object artefact)
		{
			return default(TModel);
		}

		public object Unparse(TModel model, object artefact)
		{
			return null;
		}
	}
}