using System.Collections.Generic;

namespace RTSFramework.RTSApproaches.ClassSRTS
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