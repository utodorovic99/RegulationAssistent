using RegulationAssistantChatClient.ViewModels;
using System.Windows;

namespace RegulationAssistantChatClient
{
	/// <summary>
	/// Interaction logic for the application's main window.
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindow"/> class.
		/// Sets up window components and assigns the view model to the DataContext.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainWindowViewModel();
		}
	}
}