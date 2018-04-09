using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Controller;
using RTSFramework.Controller.RunConfigurations;

namespace RTSFramework.GUI
{
	public class MainWindowViewModel :BindableBase
	{
		private string myTestValue;
		private string result;
		private ICommand startRunCommand;
		private InteractionRequest<INotification> notificationRequest;

		public MainWindowViewModel()
		{
			StartRunCommand = new DelegateCommand(StartRun);
			MyTestValue = "Test";
			NotificationRequest = new InteractionRequest<INotification>();
		}
		public InteractionRequest<INotification> NotificationRequest
		{
			get { return notificationRequest; }
			set
			{
				notificationRequest = value;
				RaisePropertyChanged();
			}
		}

		private void StartRun()
		{
			var notification = new Notification
			{
				Content = "Notification Message",
				Title = "Notification"
			};

			NotificationRequest.Raise(notification, x =>
			{
				//TODO Move!
				UnityProvider.Initialize();

				GitExampleRun();
				//TFS2010ExampleRun();
				MyTestValue = "Done!";
			});
		}

		public string Result
		{
			get { return result; }
			set
			{
				result = value;
				RaisePropertyChanged();
			}
		}

		public ICommand StartRunCommand
		{
			get { return startRunCommand; }
			set
			{
				startRunCommand = value;
				RaisePropertyChanged();
			}
		}

		public string MyTestValue
		{
			get { return myTestValue; }
			set
			{
				myTestValue = value;
				RaisePropertyChanged();
			}
		}

		#region ToDelete

		private static void SetConfig<T>(RunConfiguration<T> configuration) where T : CSharpProgramModel
		{
			configuration.ProcessingType = ProcessingType.MSTestExecutionWithoutCoverage;
			configuration.DiscoveryType = DiscoveryType.LocalDiscovery;
			configuration.GitRepositoryPath = @"C:\Git\TIATestProject\";
			configuration.AbsoluteSolutionPath = @"C:\Git\TIATestProject\TIATestProject.sln";
			configuration.RTSApproachType = RTSApproachType.ClassSRTS;
			configuration.GranularityLevel = GranularityLevel.Class;
		}

		private void TFS2010ExampleRun()
		{
			var configuration = new RunConfiguration<TFS2010ProgramModel>();
			SetConfig(configuration);

			var oldProgramModel = new TFS2010ProgramModel
			{
				VersionId = "Test"
			};
			var newProgramModel = new TFS2010ProgramModel
			{
				VersionId = "Test2"
			};
			configuration.OldProgramModel = oldProgramModel;
			configuration.NewProgramModel = newProgramModel;
			configuration.OldProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
			configuration.NewProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;

			if (configuration.GranularityLevel == GranularityLevel.File)
			{
				var controller = UnityProvider.GetTfs2010CSharpFileController();
				Result = controller.ExecuteImpactedTests(configuration);
			}
			else
			{
				var controller = UnityProvider.GetTfs2010CSharpClassController();
				Result = controller.ExecuteImpactedTests(configuration);
			}
		}

		private void GitExampleRun()
		{
			var configuration = new RunConfiguration<GitProgramModel>();
			SetConfig(configuration);

			var oldProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
				GitVersionReferenceType.LatestCommit);
			var newProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
				GitVersionReferenceType.CurrentChanges);
			configuration.OldProgramModel = oldProgramModel;
			configuration.NewProgramModel = newProgramModel;
			configuration.OldProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
			configuration.NewProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;

			if (configuration.GranularityLevel == GranularityLevel.File)
			{
				var controller = UnityProvider.GetGitCSharpFileController();
				Result = controller.ExecuteImpactedTests(configuration);
			}
			else
			{
				var controller = UnityProvider.GetGitCSharpClassController();
				Result = controller.ExecuteImpactedTests(configuration);
			}
		}

		#endregion
	}
}