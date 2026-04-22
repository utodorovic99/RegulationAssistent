using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using Microsoft.Win32;
using RegulationAssistantChatClient.ViewModels.Documents;
using RegulationAssistantChatClient.Views;
using RegulationAssistantChatClient.Properties;

namespace RegulationAssistantChatClient.Views
{
	/// <summary>
	/// Interaction logic for DocumentsView.xaml
	/// </summary>
	public partial class DocumentsView : UserControl
	{
		private DocumentsViewModel vm;

		/// <summary>
		/// Initializes new instance of <see cref="DocumentsView"/>.
		/// </summary>
		public DocumentsView()
		{
			InitializeComponent();
			vm = new DocumentsViewModel();
			DataContext = vm;
			vm.EditRequested += Vm_EditRequested;
			vm.UploadRequested += Vm_UploadRequested;
			this.Loaded += DocumentsView_Loaded;
		}

		/// <summary>
		/// Handler for the Loaded event of the view.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void DocumentsView_Loaded(object sender, RoutedEventArgs e)
		{
			var dg = this.FindName("DocumentsDataGrid") as DataGrid;
			if (dg != null)
			{
				dg.MouseDoubleClick += DataGrid_MouseDoubleClick;
			}
		}

		/// <summary>
		/// Handler for double-click event on the data grid.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (vm == null || vm.SelectedDocument == null)
			{
				return;
			}
			// Treat double-click as edit request
			Vm_EditRequested(vm.SelectedDocument);
		}

		/// <summary>
		/// Handler for requested document upload.
		/// </summary>
		private async void Vm_UploadRequested()
		{
			var dlg = new OpenFileDialog();
			dlg.Filter = "Word Documents|*.docx;*.doc|All files|*.*";
			bool? res = dlg.ShowDialog();
			if (res == true)
			{
				try
				{
					string path = dlg.FileName;
					byte[] bytes = File.ReadAllBytes(path);

					// Show DocumentInfoDialog to get title and validity period
					var infoDlg = new DocumentInfoDialog(Path.GetFileName(path));
					if (infoDlg.ShowDialog() == true)
					{
						var request = new DocumentUploadRequest
						{
							Title = infoDlg.DocumentTitle,
							ValidFrom = infoDlg.ValidFrom ?? DateTime.Now.Date,
							ValidTo = infoDlg.ValidTo,
							BuildIndex = infoDlg.BuildIndex,
							FileBytes = bytes
						};

						var uploadedDocument = await vm.UploadDocumentAsync(request);

						if (uploadedDocument == null)
						{
							MessageBox.Show(RegulationAssistantChatClient.Properties.Resources.Upload_Failed, RegulationAssistantChatClient.Properties.Resources.Upload_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format(RegulationAssistantChatClient.Properties.Resources.Upload_ErrorDetail, ex.Message), RegulationAssistantChatClient.Properties.Resources.Upload_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Handler for edit document event.
		/// </summary>
		/// <param name="doc">Document whose edit is requested.</param>
		private async void Vm_EditRequested(DocumentItemDescriptor? doc)
		{
			if (doc == null) return;

			try
			{
				// When editing, show dialog pre-filled with current values
				var infoDlg = new DocumentInfoDialog(doc.Title, doc.ValidFrom, doc.ValidTo);

				if (infoDlg.ShowDialog() == true)
				{
					// Request new upload as a new version with possibly updated metadata but no file changes
					var request = new DocumentUploadRequest
					{
						Title = infoDlg.DocumentTitle,
						ValidFrom = infoDlg.ValidFrom ?? doc.ValidFrom,
						ValidTo = infoDlg.ValidTo ?? doc.ValidTo,
						BuildIndex = infoDlg.BuildIndex,
						FileBytes = Array.Empty<byte>() // No file content change in metadata-only edit
					};

					var uploaded = await vm.UploadDocumentAsync(request);

					if (uploaded == null)
					{
						MessageBox.Show(RegulationAssistantChatClient.Properties.Resources.Upload_Failed, RegulationAssistantChatClient.Properties.Resources.Upload_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(RegulationAssistantChatClient.Properties.Resources.Upload_ErrorDetail, ex.Message), RegulationAssistantChatClient.Properties.Resources.Upload_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}