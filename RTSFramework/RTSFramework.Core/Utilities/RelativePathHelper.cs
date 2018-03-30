using System;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Core.Utilities
{
    public static class RelativePathHelper
    {
        public static string GetRelativePath<TP>(TP targetModel, string absolutePath) where TP : IProgramModel
        {
            Uri fullUri = new Uri(absolutePath, UriKind.Absolute);
            Uri relRoot = new Uri(targetModel.RootPath, UriKind.Absolute);

            return relRoot.MakeRelativeUri(fullUri).ToString();
        }
    }
}