namespace RTSFramework.Contracts.Models
{
    public class CoverageDataEntry
    {
        public string MethodName { get; set; }

        public string ClassName { get; set; }

        public string FileName { get; set; }

        public string TestCaseId { get; set; }
    }
}