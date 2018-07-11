using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.CorrespondenceModel.Models
{
    public class CorrespondenceModel : ICorrespondenceModel
	{
		public string TestType { get; set; }

        public string ProgramVersionId { get; set; }

        public Dictionary<string, HashSet<string>> CorrespondenceModelLinks { get; set; }
            = new Dictionary<string, HashSet<string>>();

        public CorrespondenceModel CloneModel(string newId)
        {
            var clone = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, HashSet<string>> testcaseRelatedElements in CorrespondenceModelLinks)
            {
                clone.Add(testcaseRelatedElements.Key, new HashSet<string>(testcaseRelatedElements.Value));
            }

            return new CorrespondenceModel{ProgramVersionId = newId, CorrespondenceModelLinks = clone };
        }

        public void UpdateByNewLinks(Dictionary<string, HashSet<string>> newLinks)
        {
            foreach (KeyValuePair<string, HashSet<string>> linksForTestcase in newLinks)
            {
                if (!CorrespondenceModelLinks.ContainsKey(linksForTestcase.Key))
                {
                    CorrespondenceModelLinks.Add(linksForTestcase.Key, linksForTestcase.Value);
                }
                else
                {
                    CorrespondenceModelLinks[linksForTestcase.Key] = linksForTestcase.Value;
                }
            }
        }

        public void RemoveDeletedTests(IEnumerable<string> allTests)
        {
            var deletedTests = CorrespondenceModelLinks.Where(x => !allTests.Contains(x.Key)).Select(x => x.Key).ToList();

            foreach (var deletedTest in deletedTests)
            {
                CorrespondenceModelLinks.Remove(deletedTest);
            }
        }
    }
}