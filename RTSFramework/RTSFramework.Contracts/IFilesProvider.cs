using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts
{
    public interface IFilesProvider<TP> where TP : IProgramModel
    {
        string GetFileContent(TP programVersion, string fullPath);
    }
}