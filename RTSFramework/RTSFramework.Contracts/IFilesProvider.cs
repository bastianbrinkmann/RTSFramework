using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
    public interface IFilesProvider<TModel> where TModel : IProgramModel
    {
        string GetFileContent(TModel programVersion, string path);

        List<string> GetAllFiles(TModel programModel);
    }
}