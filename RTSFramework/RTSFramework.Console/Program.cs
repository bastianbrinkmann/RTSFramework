using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Core;
using Unity;

namespace RTSFramework.Console
{
	class Program
	{
		static void Main(string[] args)
		{
		    string solutionFile = @"C:\Git\TIATestProject\TIATestProject.sln";

            var container = new UnityContainer();

		    VisualStudioOnlineTestDiscoveryWorkflow.InitializeComponents(container);

		    var onlineController = container.Resolve<OnlineController<OperationalDelta<CSharpDocument>, CSharpDocument, CSharpProgram, MSTestTestcase>>();

		}
	}
}
