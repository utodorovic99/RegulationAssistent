using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using Microsoft.Win32;
using RegulationAssistantChatClient.ViewModels.Documents;

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

					var request = new DocumentUploadRequest
					{
						Title = Path.GetFileName(path),
						ValidFrom = DateTime.Now.Date,
						FileBytes = bytes
					};

					var uploadedDocument = await vm.UploadDocumentAsync(request);
					
					if (uploadedDocument == null)
					{
						MessageBox.Show("Failed to upload document to the service.", "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error uploading document: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Handler for edit document event.
		/// </summary>
		/// <param name="doc">Document whose edit is requested.</param>
		private void Vm_EditRequested(DocumentItemDescriptor? doc)
		{
			// The document will be opened in Microsoft Word directly via the view model
			// No need to show a dialog anymore
		}
	}
}