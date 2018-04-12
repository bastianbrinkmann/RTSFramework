using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using RTSFramework.Concrete.User;
using RTSFramework.ViewModels.RequireUIServices;

namespace RTSFramework.ViewModels
{
	public class IntendedChangesDialogViewModel : BindableBase
	{
		private readonly IDialogService dialogService;
		private readonly IIntendedChangesProvider intendedChangesProvider;

		#region BackingFields

		private string selectedFile;
		private ObservableCollection<string> intendedChanges;
		private ICommand addFileCommand;
		private DelegateCommand removeFileCommand;

		#endregion

		private const string FileExtension = "Class Files (*.cs)|*.cs";

		public IntendedChangesDialogViewModel(IDialogService dialogService, IIntendedChangesProvider intendedChangesProvider)
		{
			this.dialogService = dialogService;
			this.intendedChangesProvider = intendedChangesProvider;

			IntendedChanges = new ObservableCollection<string>(intendedChangesProvider.IntendedChanges);
			IntendedChanges.CollectionChanged += IntendedChangesOnCollectionChanged;

			AddFileCommand = new DelegateCommand(AddFile);
			RemoveFileCommand = new DelegateCommand(RemoveFile, () => SelectedFile != null);

			PropertyChanged += OnPropertyChanged;
		}

		private void IntendedChangesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
		{
			intendedChangesProvider.IntendedChanges = IntendedChanges.ToList();
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName == nameof(SelectedFile))
			{
				RemoveFileCommand.RaiseCanExecuteChanged();
			}
		}

		private void RemoveFile()
		{
			IntendedChanges.Remove(SelectedFile);
		}

		private void AddFile()
		{
			string fileToAdd;
			if(dialogService.SelectFile(RootDirectory, FileExtension,out fileToAdd))
			{
				if (!IntendedChanges.Contains(fileToAdd))
				{
					IntendedChanges.Add(fileToAdd);
				}
			}
		}

		#region Properties

		public string SelectedFile
		{
			get { return selectedFile; }
			set
			{
				selectedFile = value;
				RaisePropertyChanged();
			}
		}

		public string RootDirectory { get; set; }

		public DelegateCommand RemoveFileCommand
		{
			get { return removeFileCommand; }
			set
			{
				removeFileCommand = value;
				RaisePropertyChanged();
			}
		}

		public ICommand AddFileCommand
		{
			get { return addFileCommand; }
			set
			{
				addFileCommand = value;
				RaisePropertyChanged();
			}
		}

		public ObservableCollection<string> IntendedChanges
		{
			get { return intendedChanges; }
			set
			{
				intendedChanges = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}
}