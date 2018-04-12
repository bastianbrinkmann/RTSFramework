using System.Collections.Generic;

namespace RTSFramework.Contracts.Models.Delta
{
    public class StructuralDelta<TModel, TModelElement> : IDelta<TModel> where TModel : IProgramModel where TModelElement : IProgramModelElement
    {
	    public StructuralDelta(TModel source, TModel target)
	    {
		    SourceModel = source;
		    TargetModel = target;
	    }

        public List<TModelElement> AddedElements { get; } = new List<TModelElement>();

        public List<TModelElement> DeletedElements { get; } = new List<TModelElement>();

        public List<TModelElement> ChangedElements { get; } = new List<TModelElement>();
        public TModel SourceModel { get; }
        public TModel TargetModel { get;}
    }
}