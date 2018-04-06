using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace RTSFramework.Concrete.CSharp.MSTestLogger
{
	public class TestResultDto
	{
		public TimeSpan Duration { get; set; }
		public string DisplayName { get; set; }
		public TestOutcome Outcome { get; set; }
		public DateTimeOffset StartTime { get; set; }
		public DateTimeOffset EndTime { get; set; }
	}
}