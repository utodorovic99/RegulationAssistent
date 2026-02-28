using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace RegulationAssistantChatClient.ViewModels.Documents
{
	/// <summary>
	/// View model for managing a collection of documents in the UI.
	/// </summary>
	public class DocumentsViewModel : INotifyPropertyChanged
	{
		private DocumentItemDescriptor? selectedDocument;

		/// <summary>
		/// Initializes new instance of <see cref="DocumentsViewModel"/>.
		/// </summary>
		public DocumentsViewModel()
		{
			Documents = new ObservableCollection<DocumentItemDescriptor>();
			UploadCommand = new RelayCommand(OnUpload);
			NewCommand = new RelayCommand(OnNew);
			EditCommand = new RelayCommand(OnEdit, CanEdit);
			DeleteCommand = new RelayCommand(OnDelete, CanDelete);

			// sample data
			Documents.Add(new DocumentItemDescriptor { Title = "Example Policy", VersionNumber = 1, ValidFrom = DateTime.Now.Date });
		}

		/// <summary>
		/// Event that is raised when the user requests to edit a document.
		/// </summary>
		public event Action<DocumentItemDescriptor?>? EditRequested;

		/// <summary>
		/// Event that is raised when the user requests to upload a new document.
		/// </summary>
		public event Action? UploadRequested;

		/// <summary>
		/// Event for property change notifications to update the UI when view model properties change.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets the collection of documents to be displayed in the UI.
		/// </summary>
		public ObservableCollection<DocumentItemDescriptor> Documents { get; }

		/// <summary>
		/// Gets or sets the currently selected document in the UI.
		/// </summary>
		public DocumentItemDescriptor? SelectedDocument
		{
			get
			{
				return selectedDocument;
			}

			set
			{
				selectedDocument = value;
				OnPropertyChanged();
				// notify command state change
				((RelayCommand)EditCommand).RaiseCanExecuteChanged();
				((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
			}
		}

		/// <summary>
		/// Gets command for uploading document.
		/// </summary>
		public ICommand UploadCommand { get; }

		/// <summary>
		/// Gets command for creating new document.
		/// </summary>
		public ICommand NewCommand { get; }

		/// <summary>
		/// Gets command for editing an existing document.
		/// </summary>
		public ICommand EditCommand { get; }

		/// <summary>
		/// Gets command for deleting an existing document.
		/// </summary>
		public ICommand DeleteCommand { get; }

		/// <summary>
		/// Validates whether the edit command can execute.
		/// </summary>
		/// <returns><c>True</c> if edit command can be executed; otherwise returns <c>false</c>.</returns>
		private bool CanEdit()
		{
			return SelectedDocument != null;
		}

		/// <summary>
		/// Validates whether the delete command can execute.
		/// </summary>
		/// <returns><c>True</c> if delete command can be executed; otherwise returns <c>false</c>.</returns>
		private bool CanDelete()
		{
			return SelectedDocument != null;
		}

		/// <summary>
		/// Handler for the edit document command.
		/// </summary>
		private void OnEdit()
		{
			EditRequested?.Invoke(SelectedDocument);
		}

		/// <summary>
		/// Handler for the delete document command.
		/// </summary>
		private void OnDelete()
		{
			if (SelectedDocument != null)
			{
				Documents.Remove(SelectedDocument);
				SelectedDocument = null;
				// update commands
				((RelayCommand)EditCommand).RaiseCanExecuteChanged();
				((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
			}
		}

		/// <summary>
		/// Handler for the upload document command.
		/// </summary>
		private void OnUpload()
		{
			UploadRequested?.Invoke();
		}

		/// <summary>
		/// Handler for the create new document command.
		/// </summary>
		private void OnNew()
		{
		}

		/// <summary>
		/// Adds a new document item to the collection. The view should call this after completing an upload.
		/// </summary>
		public void AddDocument(DocumentItemDescriptor item)
		{
			if (item == null) return;
			Documents.Add(item);
			OnPropertyChanged(nameof(Documents));
		}

		/// <summary>
		/// Rises the <see cref="PropertyChanged"/> event to notify the UI of property value changes.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}