using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using RegulationAssistantChatClient.Services;
using RegulationAssistantChatClient.Views;

namespace RegulationAssistantChatClient.ViewModels.Documents
{
	/// <summary>
	/// View model for managing a collection of documents in the UI.
	/// </summary>
	public class DocumentsViewModel : INotifyPropertyChanged
	{
		private readonly DocumentStorageServiceProxy documentStorageProxy;
		private DocumentItemDescriptor? selectedDocument;

		/// <summary>
		/// Initializes new instance of <see cref="DocumentsViewModel"/>.
		/// </summary>
		public DocumentsViewModel()
		{
			documentStorageProxy = new DocumentStorageServiceProxy(new System.Net.Http.HttpClient());
			Documents = new ObservableCollection<DocumentItemDescriptor>();
			UploadCommand = new RelayCommand(OnUpload);
			NewCommand = new RelayCommand(OnNew);
			EditCommand = new RelayCommand(OnEdit, CanEdit);
			DeleteCommand = new RelayCommand(OnDelete, CanDelete);

			// Load documents asynchronously when the view model is created
			_ = LoadDocumentsAsync();
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
			if (SelectedDocument != null)
			{
				_ = EditDocumentAsync(SelectedDocument);
			}
		}

		/// <summary>
		/// Retrieves document bytes and opens the document in Microsoft Word.
		/// </summary>
		/// <param name="document">The document descriptor to edit.</param>
		private async Task EditDocumentAsync(DocumentItemDescriptor document)
		{
			try
			{
				var response = await documentStorageProxy.GetDocumentAsync(document.Title, document.VersionNumber);

				if (response?.FileBytes == null || response.FileBytes.Length == 0)
				{
					MessageBox.Show(
						$"Failed to retrieve document from the service.\n\n" +
						$"Title: {document.Title}\n" +
						$"Version: {document.VersionNumber}\n\n" +
						$"Please ensure the document exists in the storage.",
						"Document Not Found",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
					return;
				}

				// Create and open document with Word Interop
				var editor = new WordDocumentEditor();

				// Subscribe to events
				editor.DocumentSaved += (sender, args) =>
				{
					System.Diagnostics.Debug.WriteLine($"Document saved: {document.Title}");
				};

				editor.DocumentClosed += (sender, args) =>
				{
					System.Diagnostics.Debug.WriteLine($"Document closed. WasModified: {args.WasModified}, HasBytes: {args.FileBytes != null}");
					
					if (args.WasModified && args.FileBytes != null)
					{
						// Marshal to UI thread for MessageBox and UI updates
						System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
						{
							try
							{
								System.Diagnostics.Debug.WriteLine($"Prompting user to save document: {document.Title}");
								
								// Ask user if they want to save changes as a new version
								var result = MessageBox.Show(
									$"The document '{document.Title}' has been modified.\n\nDo you want to save changes as a new version (v{document.VersionNumber + 1})?",
									"Save Changes",
									MessageBoxButton.YesNo,
									MessageBoxImage.Question);

								if (result == MessageBoxResult.Yes)
								{
									System.Diagnostics.Debug.WriteLine($"User chose to save. Uploading document...");
									await SaveDocumentAsNewVersionAsync(document.Title, args.FileBytes);
								}
								else
								{
									System.Diagnostics.Debug.WriteLine("User chose not to save changes.");
								}
							}
							finally
							{
								// Dispose the editor to clean up resources
								editor.Dispose();
							}
						});
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("No modifications detected or no file bytes available.");
						// No modifications, just dispose
						editor.Dispose();
					}
				};

				bool opened = await editor.OpenDocumentAsync(response.FileBytes, document.Title, document.VersionNumber);

				if (!opened)
				{
					MessageBox.Show("Failed to open document in Microsoft Word.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					editor.Dispose();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Saves the modified document as a new version to the service.
		/// </summary>
		/// <param name="originalTitle">The original document title.</param>
		/// <param name="fileBytes">The modified file bytes.</param>
		private async System.Threading.Tasks.Task SaveDocumentAsNewVersionAsync(string originalTitle, byte[] fileBytes)
		{
			try
			{
				System.Diagnostics.Debug.WriteLine($"SaveDocumentAsNewVersionAsync called. Title: {originalTitle}, FileBytes length: {fileBytes?.Length}");
				
				var request = new DocumentUploadRequest
				{
					Title = originalTitle,
					ValidFrom = DateTime.Now.Date,
					FileBytes = fileBytes
				};

				var uploadedDocument = await UploadDocumentAsync(request);

				System.Diagnostics.Debug.WriteLine($"Upload completed. Success: {uploadedDocument != null}, Version: {uploadedDocument?.VersionNumber}");

				if (uploadedDocument != null)
				{
					MessageBox.Show(
						$"Document saved successfully as version {uploadedDocument.VersionNumber}.\n\nThe new version has been added to the document list.",
						"Success",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
					
					System.Diagnostics.Debug.WriteLine("Refreshing documents list...");
					// Refresh the documents list to ensure it's up to date
					await LoadDocumentsAsync();
					System.Diagnostics.Debug.WriteLine("Documents list refreshed.");
				}
				else
				{
					MessageBox.Show("Failed to save document to the service.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Exception in SaveDocumentAsNewVersionAsync: {ex}");
				MessageBox.Show($"Error saving document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Handler for the delete document command.
		/// </summary>
		private void OnDelete()
		{
			if (SelectedDocument != null)
			{
				_ = DeleteDocumentAsync(SelectedDocument);
			}
		}

		/// <summary>
		/// Deletes a document from the service and removes it from the UI collection.
		/// </summary>
		/// <param name="document">The document to delete.</param>
		private async Task DeleteDocumentAsync(DocumentItemDescriptor document)
		{
			try
			{
				// Confirm deletion with user
				var result = MessageBox.Show(
					$"Are you sure you want to delete '{document.Title}' version {document.VersionNumber}?\n\nThis action cannot be undone.",
					"Confirm Delete",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);

				if (result != MessageBoxResult.Yes)
				{
					return;
				}

				// Delete from service
				var response = await documentStorageProxy.DeleteDocumentAsync(document.Title, document.VersionNumber);

				if (response?.Success == true)
				{
					// Remove from UI collection
					Documents.Remove(document);
					SelectedDocument = null;

					// Update commands
					((RelayCommand)EditCommand).RaiseCanExecuteChanged();
					((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();

					MessageBox.Show(
						$"Document '{document.Title}' v{document.VersionNumber} deleted successfully.",
						"Success",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
				}
				else
				{
					MessageBox.Show(
						$"Failed to delete document: {response?.ErrorMessage ?? "Unknown error"}",
						"Error",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error deleting document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
			_ = CreateNewDocumentAsync();
		}

		/// <summary>
		/// Creates a new blank document in Microsoft Word.
		/// </summary>
		private async Task CreateNewDocumentAsync()
		{
			try
			{
				// Create and open a blank document with Word Interop
				var editor = new WordDocumentEditor();

				// Subscribe to events
				editor.DocumentSaved += (sender, args) =>
				{
					// Optional: Notify user that changes are being tracked
				};

				editor.DocumentClosed += (sender, args) =>
				{
					if (args.WasModified && args.FileBytes != null)
					{
						// Marshal to UI thread for MessageBox and UI updates
						System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
						{
							try
							{
								// Prompt user to provide document title and save
								string documentTitle = PromptForDocumentTitle(args.DocumentTitle);

								if (!string.IsNullOrWhiteSpace(documentTitle))
								{
									await SaveNewDocumentAsync(documentTitle, args.FileBytes);
								}
							}
							finally
							{
								// Dispose the editor to clean up resources
								editor.Dispose();
							}
						});
					}
					else
					{
						// Document was closed without saving, just dispose
						editor.Dispose();
					}
				};

				bool created = await editor.CreateNewDocumentAsync("NewDocument");

				if (!created)
				{
					MessageBox.Show("Failed to create new document in Microsoft Word.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					editor.Dispose();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error creating new document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Prompts the user to enter a title for the new document.
		/// </summary>
		/// <param name="defaultTitle">The default title to suggest.</param>
		/// <returns>The document title entered by the user, or null if cancelled.</returns>
		private string? PromptForDocumentTitle(string? defaultTitle)
		{
			// Create a simple input dialog
			var inputDialog = new Window
			{
				Title = "Save New Document",
				Width = 400,
				Height = 180,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Owner = System.Windows.Application.Current.MainWindow,
				ResizeMode = ResizeMode.NoResize
			};

			var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

			var label = new System.Windows.Controls.TextBlock
			{
				Text = "Enter a title for the document:",
				FontSize = 14,
				Margin = new Thickness(0, 0, 0, 10)
			};
			System.Windows.Controls.Grid.SetRow(label, 0);
			grid.Children.Add(label);

			var textBox = new System.Windows.Controls.TextBox
			{
				// Start empty for new documents so user must provide a title explicitly
				Text = string.Empty,
				Height = 30,
				FontSize = 14,
				Padding = new Thickness(5),
				Margin = new Thickness(0, 0, 0, 20)
			};
			System.Windows.Controls.Grid.SetRow(textBox, 1);
			grid.Children.Add(textBox);

			// Ensure the textbox has focus when dialog opens
			inputDialog.Loaded += (s, e) =>
			{
				textBox.Focus();
			};

			var buttonPanel = new System.Windows.Controls.StackPanel
			{
				Orientation = System.Windows.Controls.Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			System.Windows.Controls.Grid.SetRow(buttonPanel, 3);

			var okButton = new System.Windows.Controls.Button
			{
				Content = "Save",
				Width = 80,
				Height = 30,
				Margin = new Thickness(0, 0, 10, 0),
				IsDefault = true,
				// Start disabled until user enters a title
				IsEnabled = false
			};

			// Update button enabled state as the user types so disabled appearance (gray) is applied via app styles
			textBox.TextChanged += (s, e) =>
			{
				okButton.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
			};

			okButton.Click += (s, e) =>
			{
				if (string.IsNullOrWhiteSpace(textBox.Text))
				{
					MessageBox.Show("Please enter a document title.", "Title Required", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
				inputDialog.DialogResult = true;
				inputDialog.Close();
			};
			buttonPanel.Children.Add(okButton);

			var cancelButton = new System.Windows.Controls.Button
			{
				Content = "Cancel",
				Width = 80,
				Height = 30,
				IsCancel = true
			};
			cancelButton.Click += (s, e) =>
			{
				inputDialog.DialogResult = false;
				inputDialog.Close();
			};
			buttonPanel.Children.Add(cancelButton);

			grid.Children.Add(buttonPanel);
			inputDialog.Content = grid;

			bool? dialogResult = inputDialog.ShowDialog();

			return dialogResult == true ? textBox.Text : null;
		}

		/// <summary>
		/// Saves a new document to the service.
		/// </summary>
		/// <param name="title">The title for the new document.</param>
		/// <param name="fileBytes">The document file bytes.</param>
		private async Task SaveNewDocumentAsync(string title, byte[] fileBytes)
		{
			try
			{
				var request = new DocumentUploadRequest
				{
					Title = title,
					ValidFrom = DateTime.Now.Date,
					FileBytes = fileBytes
				};

				var uploadedDocument = await UploadDocumentAsync(request);

				if (uploadedDocument != null)
				{
					MessageBox.Show(
						$"Document '{title}' saved successfully as version {uploadedDocument.VersionNumber}.\n\nThe document has been added to the document list.",
						"Success",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
					
					// Refresh the documents list to ensure it's up to date
					await LoadDocumentsAsync();
				}
				else
				{
					MessageBox.Show("Failed to save document to the service.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error saving document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
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
		/// Uploads a document to the service via API Gateway.
		/// </summary>
		/// <param name="request">The document upload request containing metadata and file bytes.</param>
		/// <returns>The uploaded document descriptor if successful; otherwise null.</returns>
		public async Task<DocumentItemDescriptor?> UploadDocumentAsync(DocumentUploadRequest request)
		{
			try
			{
				System.Diagnostics.Debug.WriteLine($"UploadDocumentAsync called for: {request.Title}");
				
				DocumentUploadResponse? response = await documentStorageProxy.UploadDocumentAsync(request);
				
				System.Diagnostics.Debug.WriteLine($"Upload response received. Success: {response?.DocumentDescriptor != null}");
				
				if (response?.DocumentDescriptor != null)
				{
					await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
					{
						System.Diagnostics.Debug.WriteLine($"Adding document to collection: {response.DocumentDescriptor.Title} v{response.DocumentDescriptor.VersionNumber}");
						Documents.Add(response.DocumentDescriptor);
						OnPropertyChanged(nameof(Documents));
					});
					return response.DocumentDescriptor;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Exception in UploadDocumentAsync: {ex}");
			}

			return null;
		}

		/// <summary>
		/// Loads all documents from the service via API Gateway.
		/// </summary>
		private async Task LoadDocumentsAsync()
		{
			try
			{
				System.Diagnostics.Debug.WriteLine("Loading documents from service...");
				
				var documents = await documentStorageProxy.GetAllDocumentsAsync();
				
				System.Diagnostics.Debug.WriteLine($"Received {documents?.Count ?? 0} documents from service.");
				
				await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
				{
					Documents.Clear();

					foreach (var doc in documents)
					{
						System.Diagnostics.Debug.WriteLine($"Adding to UI: {doc.Title} v{doc.VersionNumber}");
						Documents.Add(doc);
					}

					OnPropertyChanged(nameof(Documents));
					System.Diagnostics.Debug.WriteLine($"UI collection now has {Documents.Count} documents.");
				});
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Exception in LoadDocumentsAsync: {ex}");
			}
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