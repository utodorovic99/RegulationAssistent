using System.Windows;

namespace RegulationAssistantChatClient.Views
{
	/// <summary>
	/// Interaction logic for DocumentTitleDialog.xaml
	/// </summary>
	public partial class DocumentTitleDialog : Window
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentTitleDialog"/> class.
		/// </summary>
		/// <param name="defaultTitle">The default title to display.</param>
		public DocumentTitleDialog(string defaultTitle = "NewDocument.docx")
		{
			InitializeComponent();
			TitleTextBox.Text = defaultTitle;
			TitleTextBox.Focus();
			TitleTextBox.SelectAll();

			// Set initial enabled state of the OK/Save button and update on text change
			OkButton.IsEnabled = !string.IsNullOrWhiteSpace(TitleTextBox.Text);
			TitleTextBox.TextChanged += (s, e) =>
			{
				OkButton.IsEnabled = !string.IsNullOrWhiteSpace(TitleTextBox.Text);
			};
		}

		/// <summary>
		/// Gets the document title entered by the user.
		/// </summary>
		public string DocumentTitle => TitleTextBox.Text;

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
			{
				MessageBox.Show("Please enter a document title.", "Title Required", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
