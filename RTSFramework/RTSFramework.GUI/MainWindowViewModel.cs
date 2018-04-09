﻿using System;
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
using RTSFramework.Controller;
using RTSFramework.Controller.RunConfigurations;

namespace RTSFramework.GUI
{
	public class MainWindowViewModel :BindableBase
	{
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>> gitFileController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, GitProgramModel, MSTestTestcase>> gitClassController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>> tfsFileController;
		private readonly Lazy<CSharpProgramModelFileRTSController<CSharpClassElement, TFS2010ProgramModel, MSTestTestcase>> tfsClassController;

		private string myTestValue;
		private string result;
		private ICommand startRunCommand;
		private InteractionRequest<INotification> notificationRequest;

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
			MyTestValue = "Done!";
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