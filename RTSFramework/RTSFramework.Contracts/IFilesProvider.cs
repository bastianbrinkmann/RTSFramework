using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
    public interface IFilesProvider
    {
        string GetFileContent(IProgramModel programVersion, string path);

        List<string> GetAllFiles(IProgramModel programModel);
    }
}