using System;
using System.Windows;
using System.Windows.Controls;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace RegulationAssistantChatClient.Views
{
	/// <summary>
	/// Interaction logic for DocumentEditView.xaml
	/// </summary>
	public partial class DocumentEditView : UserControl
	{
		/// <summary>
		/// Initializes a new instance of <see cref="DocumentEditView"/>.
		/// </summary>
		/// <param name="document">Document being edited.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> is <c>null</c>.</exception>
		public DocumentEditView(DocumentItemDescriptor document)
		{
			InitializeComponent();
			ArgumentNullException.ThrowIfNull(document, nameof(document));
			DataContext = Document;

			OkButton.Click += OkButton_Click;
			CancelButton.Click += CancelButton_Click;
		}

		/// <summary>
		/// Gets document which is being edited.
		/// </summary>
		public DocumentItemDescriptor Document { get; }

		/// <summary>
		/// Handler for OK button click event.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			// close parent window with DialogResult = true
			if (Window.GetWindow(this) is Window win)
			{
				win.DialogResult = true;
				win.Close();
			}
		}

		/// <summary>
		/// Handler for cancel button click event.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (Window.GetWindow(this) is Window win)
			{
				win.DialogResult = false;
				win.Close();
			}
		}
	}
}