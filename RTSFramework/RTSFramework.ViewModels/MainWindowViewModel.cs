using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>> gitFileController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, GitProgramModel, MSTestTestcase>> gitClassController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>> tfsFileController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, TFS2010ProgramModel, MSTestTestcase>> tfsClassController;

		private string result;
		private ICommand startRunCommand;
		private InteractionRequest<INotification> notificationRequest;
		private ProcessingType processingType;
		private DiscoveryType discoveryType;
		private RTSApproachType rtsApproachType;
		private GranularityLevel granularityLevel;
		private bool isGranularityLevelChangable;
		private string solutionFilePath;

		public MainWindowViewModel(
			Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>> gitFileController,
			Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, GitProgramModel, MSTestTestcase>> gitClassController,
			Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>> tfsFileController,
			Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, TFS2010ProgramModel, MSTestTestcase>> tfsClassController)
		{
			this.gitFileController = gitFileController;
			this.gitClassController = gitClassController;
			this.tfsFileController = tfsFileController;
			this.tfsClassController = tfsClassController;

			StartRunCommand = new DelegateCommand(StartRun);
			NotificationRequest = new InteractionRequest<INotification>();

			//Defaults
			DiscoveryType = DiscoveryType.LocalDiscovery;
			ProcessingType = ProcessingType.MSTestExecution;
			RTSApproachType = RTSApproachType.ClassSRTS;
			GranularityLevel = GranularityLevel.Class;
			SolutionFilePath = @"C:\Git\TIATestProject\TIATestProject.sln";

			PropertyChanged += OnPropertyChanged;
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName == nameof(RTSApproachType))
			{
				if (RTSApproachType == RTSApproachType.ClassSRTS)
				{
					GranularityLevel = GranularityLevel.Class;
				}
				IsGranularityLevelChangable = RTSApproachType != RTSApproachType.ClassSRTS;
			}
		}

		public string SolutionFilePath
		{
			get { return solutionFilePath; }
			set
			{
				solutionFilePath = value;
				RaisePropertyChanged();
			}
		}

		public bool IsGranularityLevelChangable
		{
			get { return isGranularityLevelChangable; }
			set
			{
				isGranularityLevelChangable = value;
				RaisePropertyChanged();
			}
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

		private async void StartRun()
		{
			var notification = new Notification
			{
				Content = "Starting Run!",
				Title = "Notification"
			};

			NotificationRequest.Raise(notification);
			await GitExampleRun();
			//TFS2010ExampleRun();
		}

		public GranularityLevel GranularityLevel
		{
			get { return granularityLevel; }
			set
			{
				granularityLevel = value;
				RaisePropertyChanged();
			}
		}

		public RTSApproachType RTSApproachType
		{
			get { return rtsApproachType; }
			set
			{
				rtsApproachType = value;
				RaisePropertyChanged();
			}
		}

		public DiscoveryType DiscoveryType
		{
			get { return discoveryType; }
			set
			{
				discoveryType = value;
				RaisePropertyChanged();
			}
		}

		public ProcessingType ProcessingType
		{
			get { return processingType; }
			set
			{
				processingType = value;
				RaisePropertyChanged();
			}
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

		#region ToDelete

		private void SetConfig<T>(RunConfiguration<T> configuration) where T : CSharpProgramModel
		{
			configuration.ProcessingType = ProcessingType;
			configuration.DiscoveryType = DiscoveryType;
			configuration.GitRepositoryPath = @"C:\Git\TIATestProject\";
			configuration.AbsoluteSolutionPath = SolutionFilePath;
			configuration.RTSApproachType = RTSApproachType;
			configuration.GranularityLevel = GranularityLevel;
		}

		private async Task TFS2010ExampleRun()
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
				Result = await Task.Run(() => tfsFileController.Value.ExecuteImpactedTests(configuration));
			}
			else
			{
				Result = await Task.Run(() => tfsClassController.Value.ExecuteImpactedTests(configuration));
			}
		}

		private async Task GitExampleRun()
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
				Result = await Task.Run(() => gitFileController.Value.ExecuteImpactedTests(configuration));
			}
			else
			{
				Result = await Task.Run(() => gitClassController.Value.ExecuteImpactedTests(configuration));
			}
		}

		#endregion
	}
}