using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Contracts;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.TFS2010
{
	//TODO Replace by TFS Files Provider
    public class LocalFilesProvider : IFilesProvider<TFS2010ProgramModel>
    {
        public string GetFileContent(TFS2010ProgramModel programModel, string path)
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

        public List<string> GetAllFiles(TFS2010ProgramModel programModel)
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