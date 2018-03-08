using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Core;
using Unity;
using Unity.RegistrationByConvention;

namespace RTSFramework.Concrete.CSharp
{
    public static class VisualStudioOnlineTestDiscoveryWorkflow
    {
        public static void InitializeComponents(IUnityContainer container)
        {
            container.RegisterType<IOnlineDeltaDiscoverer<CSharpProgram, CSharpDocument, OperationalDelta<CSharpDocument>>, VisualStudioDocumentOnlineDeltaDiscoverer>();

            container.RegisterType<OnlineController<OperationalDelta<CSharpDocument>, CSharpDocument, CSharpProgram, MSTestTestcase>>();
        }
    }
}