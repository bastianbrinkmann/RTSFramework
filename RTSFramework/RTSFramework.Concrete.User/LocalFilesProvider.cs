using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.User
{
    public class LocalFilesProvider<TP> : IFilesProvider<TP> where TP : IProgramModel
    {
        public string GetFileContent(TP programModel, string path)
        {
	        Uri fullUri;
            if (!Uri.TryCreate(path, UriKind.Absolute, out fullUri))
            {
                var absolutePath = RelativePathHelper.GetAbsolutePath(programModel, path);
                fullUri = new Uri(absolutePath, UriKind.Absolute);
            }

	        using (var fileStream = File.OpenRead(fullUri.LocalPath))
	        {
				using (var tr = new StreamReader(fileStream, Encoding.UTF8))
				{
					return tr.ReadToEnd();
				}
			}
        }

        public List<string> GetAllFiles(TP programModel)
        {
			return GetAllFiles(programModel.RootPath);
        }

	    private List<string> GetAllFiles(string directory)
	    {
		    var result = Directory.GetFiles(directory).ToList();

		    foreach (var subDirectory in Directory.GetDirectories(directory))
		    {
				result.AddRange(GetAllFiles(subDirectory));
			}

		    return result;
	    } 
    }
}