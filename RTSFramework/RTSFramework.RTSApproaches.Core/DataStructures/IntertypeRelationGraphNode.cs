namespace RTSFramework.RTSApproaches.Core.DataStructures
{
    public class IntertypeRelationGraphNode
    {
        public IntertypeRelationGraphNode(string typeIdentifier)
        {
            TypeIdentifier = typeIdentifier;
        }

        public string TypeIdentifier { get; }
    }
}