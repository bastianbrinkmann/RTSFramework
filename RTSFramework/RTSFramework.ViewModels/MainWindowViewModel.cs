﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
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
		private readonly Lazy<CSharpProgramModelFileRTSController<MSTestTestcase>> executionController;
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
		private ICommand cancelRunCommand;

		public MainWindowViewModel(Lazy<CSharpProgramModelFileRTSController<MSTestTestcase>> executionController, IDialogService dialogService)
		{
			this.executionController = executionController;
			this.dialogService = dialogService;

			StartRunCommand = new DelegateCommand(StartRun);
			CancelRunCommand = new DelegateCommand(CancelRun);
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

		private CancellationTokenSource cancellationTokenSource;

		private void CancelRun()
		{
			cancellationTokenSource?.Cancel();
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName == nameof(RTSApproachType))
			{
				if (RTSApproachType == RTSApproachType.ClassSRTS)
				{
					GranularityLevel = GranularityLevel.Class;
				}
				IsGranularityLevelChangable = RTSApproachType == RTSApproachType.DynamicRTS;
			}

			if (propertyChangedEventArgs.PropertyName == nameof(ProgramModelType))
			{
				IsGitRepositoryPathChangable = ProgramModelType == ProgramModelType.GitProgramModel;
			}
		}

		#region Properties

		public ICommand CancelRunCommand
		{
			get { return cancelRunCommand; }
			set
			{
				cancelRunCommand = value;
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
			IsRunning = true;
			cancellationTokenSource = new CancellationTokenSource();

			try
			{
				CSharpProgramModel oldProgramModel, newProgramModel;

				if (ProgramModelType == ProgramModelType.GitProgramModel)
				{
					oldProgramModel = GitProgramModelProvider.GetGitProgramModel(GitRepositoryPath, GitVersionReferenceType.LatestCommit);
					newProgramModel = GitProgramModelProvider.GetGitProgramModel(GitRepositoryPath, GitVersionReferenceType.CurrentChanges);
				}
				else
				{
					if (DiscoveryType == DiscoveryType.LocalDiscovery)
					{
						dialogService.ShowError("Local Discovery combined with TFS 2010 is not supported yet!");
						return;
					}

					oldProgramModel = new TFS2010ProgramModel {VersionId = "Test"};
					newProgramModel = new TFS2010ProgramModel {VersionId = "Test2"};
				}

				await ExampleRun(oldProgramModel, newProgramModel);

				dialogService.ShowInformation(Result, "Run Result");
			}
			catch (Exception e)
			{
				dialogService.ShowError(e.Message);
			}
			finally
			{
				IsRunning = false;
			}
		}

		private async Task ExampleRun(CSharpProgramModel oldProgramModel, CSharpProgramModel newProgramModel)
		{
			var configuration = new RunConfiguration();
			SetConfig(configuration, oldProgramModel, newProgramModel);

			Result = await Task.Run(() => executionController.Value.ExecuteImpactedTests(configuration, cancellationTokenSource.Token), cancellationTokenSource.Token);
		}

		private void SetConfig(RunConfiguration configuration, CSharpProgramModel oldProgramModel, CSharpProgramModel newProgramModel)
		{
			configuration.ProcessingType = ProcessingType;
			configuration.DiscoveryType = DiscoveryType;
			configuration.GitRepositoryPath = GitRepositoryPath;
			configuration.AbsoluteSolutionPath = SolutionFilePath;
			configuration.RTSApproachType = RTSApproachType;
			configuration.GranularityLevel = GranularityLevel;

			oldProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
			newProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
			oldProgramModel.GranularityLevel = GranularityLevel;
			newProgramModel.GranularityLevel = GranularityLevel;

			configuration.OldProgramModel = oldProgramModel;
			configuration.NewProgramModel = newProgramModel;
		}
	}
}