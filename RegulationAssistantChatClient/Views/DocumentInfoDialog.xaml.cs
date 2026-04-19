using System;
using System.Windows;

namespace RegulationAssistantChatClient.Views
{
	/// <summary>
	/// Interaction logic for DocumentInfoDialog.xaml
	/// </summary>
	public partial class DocumentInfoDialog : Window
	{
		public DocumentInfoDialog(string defaultTitle = "NewDocument.docx", DateTime? validFrom = null, DateTime? validTo = null)
		{
			InitializeComponent();
			TitleTextBox.Text = defaultTitle;
			TitleTextBox.Focus();
			TitleTextBox.SelectAll();

			// Default validity period: ValidFrom = now, ValidTo = far future
			ValidFromPicker.SelectedDate = validFrom ?? DateTime.Now;
			// Use a very distant year for indefinite validity
			ValidToPicker.SelectedDate = validTo ?? DateTime.MaxValue;

			OkButton.IsEnabled = !string.IsNullOrWhiteSpace(TitleTextBox.Text);
			TitleTextBox.TextChanged += (s, e) => OkButton.IsEnabled = !string.IsNullOrWhiteSpace(TitleTextBox.Text);
		}

		public string DocumentTitle => TitleTextBox.Text;
		public DateTime? ValidFrom => ValidFromPicker.SelectedDate;
		public DateTime? ValidTo => ValidToPicker.SelectedDate;

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
			{
				MessageBox.Show("Unesite naslov dokumenta.", "Potreban naslov", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (ValidFrom != null && ValidTo != null && ValidFrom > ValidTo)
			{
				MessageBox.Show("Datum 'Vazi od' ne moze biti posle 'Vazi do'.", "Neispravan period", MessageBoxButton.OK, MessageBoxImage.Warning);
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
