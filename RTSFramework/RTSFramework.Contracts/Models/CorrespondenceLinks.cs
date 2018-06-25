using System;
using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
    public class CorrespondenceLinks
    {
        public CorrespondenceLinks(HashSet<Tuple<string, string>> links)
        {
            Links = links;
        }

        public HashSet<Tuple<string, string>> Links { get; }
    }
}