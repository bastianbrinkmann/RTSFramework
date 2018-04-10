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
using RTSFramework.ViewModels.Utilities;

namespace RTSFramework.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>> gitFileController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, GitProgramModel, MSTestTestcase>> gitClassController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>> tfsFileController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, TFS2010ProgramModel, MSTestTestcase>> tfsClassController;
		private readonly IDialogService dialogService;

		private string result;
		private ICommand startRunCommand;
		private ProcessingType processingType;
		private DiscoveryType discoveryType;
		private RTSApproachType rtsApproachType;
		private GranularityLevel granularityLevel;
		private bool isGranularityLevelChangable;
		private string solutionFilePath;
		private string gitRepositoryPath;
		private bool isGitRepositoryPathChangable;
		private ProgramModelType programModelType;
		private bool isRunning;

		public MainWindowViewModel(
			Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>> gitFileController,
			Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, GitProgramModel, MSTestTestcase>> gitClassController,
			Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>> tfsFileController,
			Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, TFS2010ProgramModel, MSTestTestcase>> tfsClassController,
			IDialogService dialogService)
		{
			this.gitFileController = gitFileController;
			this.gitClassController = gitClassController;
			this.tfsFileController = tfsFileController;
			this.tfsClassController = tfsClassController;
			this.dialogService = dialogService;

			StartRunCommand = new DelegateCommand(StartRun);
			IsRunning = false;

			//Defaults
			DiscoveryType = DiscoveryType.LocalDiscovery;
			ProcessingType = ProcessingType.MSTestExecution;
			RTSApproachType = RTSApproachType.ClassSRTS;
			GranularityLevel = GranularityLevel.Class;
			IsGranularityLevelChangable = false;
			SolutionFilePath = @"C:\Git\TIATestProject\TIATestProject.sln";
			ProgramModelType = ProgramModelType.GitProgramModel;
			GitRepositoryPath = @"C:\Git\TIATestProject\";
			IsGitRepositoryPathChangable = true;

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
				IsGranularityLevelChangable = RTSApproachType == RTSApproachType.RetestAll || RTSApproachType == RTSApproachType.DynamicRTS;
			}

			if (propertyChangedEventArgs.PropertyName == nameof(ProgramModelType))
			{
				IsGitRepositoryPathChangable = ProgramModelType == ProgramModelType.GitProgramModel;
			}
		}

		#region Properties

		public bool IsRunning
		{
			get { return isRunning; }
			set
			{
				isRunning = value;
				RaisePropertyChanged();
			}
		}

		public ProgramModelType ProgramModelType
		{
			get { return programModelType; }
			set
			{
				programModelType = value;
				RaisePropertyChanged();
			}
		}

		public bool IsGitRepositoryPathChangable
		{
			get { return isGitRepositoryPathChangable; }
			set
			{
				isGitRepositoryPathChangable = value;
				RaisePropertyChanged();
			}
		}

		public string GitRepositoryPath
		{
			get { return gitRepositoryPath; }
			set
			{
				gitRepositoryPath = value;
				RaisePropertyChanged();
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

		#endregion

		private async void StartRun()
		{
			if (ProgramModelType == ProgramModelType.GitProgramModel)
			{
				await GitExampleRun();
			}
			else
			{
				if (DiscoveryType == DiscoveryType.LocalDiscovery)
				{
					dialogService.ShowErrorMessage("Local Discovery combined with TFS 2010 is not supported yet!");
					return;
				}

				await TFS2010ExampleRun();
			}
		}

		private void SetConfig<T>(RunConfiguration<T> configuration) where T : CSharpProgramModel
		{
			configuration.ProcessingType = ProcessingType;
			configuration.DiscoveryType = DiscoveryType;
			configuration.GitRepositoryPath = GitRepositoryPath;
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

			try
			{
				if (configuration.GranularityLevel == GranularityLevel.File)
				{
					Result = await Task.Run(() => tfsFileController.Value.ExecuteImpactedTests(configuration));
				}
				else
				{
					Result = await Task.Run(() => tfsClassController.Value.ExecuteImpactedTests(configuration));
				}
			}
			catch (Exception e)
			{
				dialogService.ShowErrorMessage(e.Message);
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

			try
			{
				if (configuration.GranularityLevel == GranularityLevel.File)
				{
					Result = await Task.Run(() => gitFileController.Value.ExecuteImpactedTests(configuration));
				}
				else
				{
					Result = await Task.Run(() => gitClassController.Value.ExecuteImpactedTests(configuration));
				}
			}
			catch (Exception e)
			{
				dialogService.ShowErrorMessage(e.Message);
			}
		}
	}
}