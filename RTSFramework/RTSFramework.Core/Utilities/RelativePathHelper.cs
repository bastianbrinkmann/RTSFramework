using System;
using System.IO;
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

        public static string GetAbsolutePath<TP>(TP targetModel, string relativePath) where TP : IProgramModel
        {
            return Path.Combine(targetModel.RootPath, relativePath);
        }
    }
}