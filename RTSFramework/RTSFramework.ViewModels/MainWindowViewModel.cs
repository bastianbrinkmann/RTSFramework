using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.ViewModels.RequireUIServices;
using RTSFramework.ViewModels.RunConfigurations;

namespace RTSFramework.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		private readonly IDialogService dialogService;
		private readonly GitCommitsProvider gitCommitsProvider;
		private CancellationTokenSource cancellationTokenSource;

		#region BackingFields

		private ICommand startRunCommand;
		private ProcessingType processingType;
		private DiscoveryType discoveryType;
		private RTSApproachType rtsApproachType;
		private GranularityLevel granularityLevel;
		private bool isGranularityLevelChangable;
		private string solutionFilePath;
		private string repositoryPath;
		private bool isGitRepositoryPathChangable;
		private ProgramModelType programModelType;
		private bool isRunning;
		private ICommand cancelRunCommand;
		private ObservableCollection<TestResultListViewItemViewModel> testResults;
		private RunStatus runStatus;
		private ICommand selectSolutionFileCommand;
		private ICommand selectRepositoryCommand;
		private ICommand specitfyIntendedChangesCommand;
		private bool isIntededChangesEditingEnabled;
		private CommitViewModel fromCommit;
		private CommitViewModel toCommit;
		private ObservableCollection<CommitViewModel> fromCommitModels;
		private ObservableCollection<CommitViewModel> toCommitModels;
		private bool isFromCommitChangeable;
		private bool isToCommitChangeable;

		#endregion

		public MainWindowViewModel(IDialogService dialogService, GitCommitsProvider gitCommitsProvider)
		{
			this.dialogService = dialogService;
			this.gitCommitsProvider = gitCommitsProvider;

			StartRunCommand = new DelegateCommand(ExecuteRunFixModel);
			CancelRunCommand = new DelegateCommand(CancelRun);
			SelectSolutionFileCommand = new DelegateCommand(SelectSolutionFile);
			SelectRepositoryCommand = new DelegateCommand(SelectRepository);
			SpecitfyIntendedChangesCommand = new DelegateCommand(SpecifyIntendedChanges);

			TestResults = new ObservableCollection<TestResultListViewItemViewModel>();
			FromCommitModels = new ObservableCollection<CommitViewModel>();
			ToCommitModels = new ObservableCollection<CommitViewModel>();

			RunStatus = RunStatus.Ready;

			PropertyChanged += OnPropertyChanged;

			//TODO: Defaults - Load from config
			DiscoveryType = DiscoveryType.LocalDiscovery;
			ProcessingType = ProcessingType.MSTestExecution;
			RTSApproachType = RTSApproachType.ClassSRTS;
			GranularityLevel = GranularityLevel.Class;
			IsGranularityLevelChangable = false;
			SolutionFilePath = @"C:\Git\TIATestProject\TIATestProject.sln";
			ProgramModelType = ProgramModelType.GitProgramModel;
			RepositoryPath = @"C:\Git\TIATestProject\";
			IsGitRepositoryPathChangable = true;
		}

		private void SpecifyIntendedChanges()
		{
			var intendedChangesViewModel = dialogService.OpenDialogByViewModel<IntendedChangesDialogViewModel>();

			intendedChangesViewModel.RootDirectory = Path.GetDirectoryName(SolutionFilePath);
		}

		private void SelectRepository()
		{
			string selectedDirectory;
			if (dialogService.SelectDirectory(RepositoryPath, out selectedDirectory))
			{
				RepositoryPath = selectedDirectory;
			}
		}

		private void SelectSolutionFile()
		{
			string selectedFile;
			if (dialogService.SelectFile(RepositoryPath, "Solution Files (*.sln)|*.sln", out selectedFile))
			{
				SolutionFilePath = selectedFile;
			}
		}

		private void CancelRun()
		{
			cancellationTokenSource?.Cancel();
			RunStatus = RunStatus.Cancelled;
		}

		private void RefreshCommitsSelection()
		{
			FromCommitModels.Clear();
			FromCommitModels.AddRange(gitCommitsProvider.GetAllCommits(RepositoryPath).Select(ConvertCommit));
			FromCommit =FromCommitModels.FirstOrDefault();
			IsFromCommitChangeable = DiscoveryType == DiscoveryType.VersionCompare && FromCommitModels.Any();
		}

		private CommitViewModel ConvertCommit(GitCommit gitCommit)
		{
			return new CommitViewModel
			{
				Committer = gitCommit.Committer,
				Identifier = gitCommit.ShaId,
				Message = gitCommit.Message
			};
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			switch (propertyChangedEventArgs.PropertyName)
			{
				case nameof(RTSApproachType):
					if (RTSApproachType == RTSApproachType.ClassSRTS)
					{
						GranularityLevel = GranularityLevel.Class;
					}
					IsGranularityLevelChangable = RTSApproachType == RTSApproachType.DynamicRTS;
					break;
				case nameof(ProgramModelType):
					IsGitRepositoryPathChangable = ProgramModelType == ProgramModelType.GitProgramModel;
					break;
				case nameof(DiscoveryType):
					IsIntededChangesEditingEnabled = DiscoveryType == DiscoveryType.UserIntendedChangesDiscovery;
					IsFromCommitChangeable = DiscoveryType == DiscoveryType.VersionCompare && FromCommitModels.Any();
					IsToCommitChangeable = DiscoveryType == DiscoveryType.VersionCompare && ToCommitModels.Any();
					break;
				case nameof(RunStatus):
					IsRunning = RunStatus == RunStatus.Running;
					break;
				case nameof(RepositoryPath):
					RefreshCommitsSelection();
					break;
				case nameof(FromCommit):
					var toCommitId = ToCommit?.Identifier;
					ToCommitModels.Clear();
					ToCommitModels.AddRange(gitCommitsProvider.GetAllCommits(RepositoryPath).TakeWhile(x => x.ShaId != FromCommit.Identifier).Select(ConvertCommit));
					ToCommit = ToCommitModels.SingleOrDefault(x => x.Identifier == toCommitId) ?? ToCommitModels.FirstOrDefault();
					IsToCommitChangeable = DiscoveryType == DiscoveryType.VersionCompare && ToCommitModels.Any();
					break;
			}
		}

		#region Properties

		public bool IsToCommitChangeable
		{
			get { return isToCommitChangeable; }
			set
			{
				isToCommitChangeable = value;
				RaisePropertyChanged();
			}
		}

		public bool IsFromCommitChangeable
		{
			get { return isFromCommitChangeable; }
			set
			{
				isFromCommitChangeable = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<CommitViewModel> ToCommitModels
		{
			get { return toCommitModels; }
			set
			{
				toCommitModels = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<CommitViewModel> FromCommitModels
		{
			get { return fromCommitModels; }
			set
			{
				fromCommitModels = value;
				RaisePropertyChanged();
			}
		}

		public CommitViewModel ToCommit
		{
			get { return toCommit; }
			set
			{
				toCommit = value;
				RaisePropertyChanged();
			}
		}

		public CommitViewModel FromCommit
		{
			get { return fromCommit; }
			set
			{
				fromCommit = value;
				RaisePropertyChanged();
			}
		}

		public bool IsIntededChangesEditingEnabled
		{
			get { return isIntededChangesEditingEnabled; }
			set
			{
				isIntededChangesEditingEnabled = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SpecitfyIntendedChangesCommand
		{
			get { return specitfyIntendedChangesCommand; }
			set
			{
				specitfyIntendedChangesCommand = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SelectRepositoryCommand
		{
			get { return selectRepositoryCommand; }
			set
			{
				selectRepositoryCommand = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SelectSolutionFileCommand
		{
			get { return selectSolutionFileCommand; }
			set
			{
				selectSolutionFileCommand = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<TestResultListViewItemViewModel> TestResults
		{
			get { return testResults; }
			set
			{
				testResults = value;
				RaisePropertyChanged();
			}
		}

		public ICommand CancelRunCommand
		{
			get { return cancelRunCommand; }
			set
			{
				cancelRunCommand = value;
				RaisePropertyChanged();
			}
		}

		public RunStatus RunStatus
		{
			get { return runStatus; }
			set
			{
				runStatus = value;
				RaisePropertyChanged();
			}
		}

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

		public string RepositoryPath
		{
			get { return repositoryPath; }
			set
			{
				repositoryPath = value;
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

		private async void ExecuteRunFixModel()
		{
			RunStatus = RunStatus.Running;
			TestResults.Clear();

			cancellationTokenSource = new CancellationTokenSource();

			try
			{
				switch (ProgramModelType)
				{
					case ProgramModelType.GitProgramModel:
						await ExecuteGitRun();
						break;
					case ProgramModelType.TFS2010ProgramModel:
						await ExecuteTFS2010Run();
						break;
				}

				RunStatus = RunStatus.Completed;
			}
			catch (Exception e)
			{
				RunStatus = RunStatus.Failed;
				dialogService.ShowError(e.Message);
			}
		}

		private async Task ExecuteGitRun()
		{
			GitVersionIdentification oldGitIdentification, newGitIdentification;

			if (DiscoveryType == DiscoveryType.VersionCompare)
			{
				oldGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.SpecificCommit,
					Commit = new GitCommit { ShaId = FromCommit.Identifier},
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath,
					GranularityLevel = GranularityLevel
				};
				newGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.SpecificCommit,
					Commit = new GitCommit { ShaId = ToCommit.Identifier },
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath,
					GranularityLevel = GranularityLevel
				};
			}
			else
			{
				oldGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.LatestCommit,
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath,
					GranularityLevel = GranularityLevel
				};
				newGitIdentification = new GitVersionIdentification
				{
					ReferenceType = GitVersionReferenceType.CurrentChanges,
					RepositoryPath = RepositoryPath,
					AbsoluteSolutionPath = SolutionFilePath,
					GranularityLevel = GranularityLevel
				};
			}
			


			await ExecuteRunFixGranularityLevel<GitVersionIdentification, GitProgramModel>(oldGitIdentification, newGitIdentification);
		}

		private async Task ExecuteTFS2010Run()
		{
			if (DiscoveryType == DiscoveryType.LocalDiscovery || DiscoveryType == DiscoveryType.VersionCompare)
			{
				throw new ArgumentException("Commit based changes combined with TFS 2010 are not supported yet!");
			}

			var oldTfsProgramArtefact = new TFS2010VersionIdentification
			{
				AbsoluteSolutionPath = SolutionFilePath,
				GranularityLevel = GranularityLevel,
				CommitId = "Test"
			};
			var newTfsProgramArtefact = new TFS2010VersionIdentification
			{
				AbsoluteSolutionPath = SolutionFilePath,
				GranularityLevel = GranularityLevel,
				CommitId = "Test2"
			};

			await ExecuteRunFixGranularityLevel<TFS2010VersionIdentification, TFS2010ProgramModel>(oldTfsProgramArtefact, newTfsProgramArtefact);
		}

		private async Task ExecuteRunFixGranularityLevel<TArtefact, TModel>(TArtefact oldProgramArtefact, TArtefact newProgramArtefact)
			where TModel : CSharpProgramModel
		{
			switch (GranularityLevel)
			{
				case GranularityLevel.File:
					await ExecuteRunFixProcessingType<TArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(oldProgramArtefact, newProgramArtefact);
					break;
				case GranularityLevel.Class:
					await ExecuteRunFixProcessingType<TArtefact, TModel, StructuralDelta<TModel, CSharpClassElement>>(oldProgramArtefact, newProgramArtefact);
					break;
			}
		}

		private async Task ExecuteRunFixProcessingType<TArtefact, TModel, TDelta>(TArtefact oldArtefact, TArtefact newArtefact)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
		{
			switch (ProcessingType)
			{
				case ProcessingType.MSTestExecution:
				case ProcessingType.MSTestExecutionWithCoverage:
					var executionResult = await ExecuteRun<TArtefact, TModel, TDelta, MSTestTestcase, MSTestExectionResult>(oldArtefact, newArtefact);

					TestResults.Clear();
					TestResults.AddRange(executionResult.TestcasesResults.Select(x => new TestResultListViewItemViewModel
					{
						FullyQualifiedName = x.TestCase.Id,
						Categories = string.Join(",", x.TestCase.Categories),
						TestOutcome = x.Outcome
					}));
					break;
				case ProcessingType.CsvReporting:
					var csvCreationResult = await ExecuteRun<TArtefact, TModel, TDelta, MSTestTestcase, FileProcessingResult>(oldArtefact, newArtefact);
					bool openFile = dialogService.ShowQuestion($"CSV file was created at '{csvCreationResult.FilePath}'.{Environment.NewLine} Do you want to open the file?","CSV File Created");
					if (openFile)
					{
						Process.Start(csvCreationResult.FilePath);
					}

					break;
				case ProcessingType.ListReporting:
					var listReportingResult = await ExecuteRun<TArtefact, TModel, TDelta, MSTestTestcase, TestListResult<MSTestTestcase>>(oldArtefact, newArtefact);
					TestResults.Clear();
					TestResults.AddRange(listReportingResult.IdentifiedTests.Select(x => new TestResultListViewItemViewModel
					{
						FullyQualifiedName = x.Id,
						Categories = string.Join(",", x.Categories)
					}));
					break;
			}
		}

		private async Task<TResult> ExecuteRun<TArtefact, TModel, TDelta, TTestCase, TResult>(TArtefact oldArtefact, TArtefact newArtefact) where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var stateBasedController = UnityModelInitializer.GetStateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>(DiscoveryType, RTSApproachType, ProcessingType);

			return
				await Task.Run(
					() => stateBasedController.ExecuteImpactedTests(oldArtefact, newArtefact, cancellationTokenSource.Token),
					cancellationTokenSource.Token);
		}
	}
}