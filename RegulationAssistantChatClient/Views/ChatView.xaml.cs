using System.Windows.Controls;

namespace RegulationAssistantChatClient.Views
{
	/// <summary>
	/// Interaction logic for the chat view user control. Hosts the chat UI and is bound to a view model
	/// that provides messages, commands and options.
	/// </summary>
	public partial class ChatView : UserControl
	{
		/// <summary>
		/// Creates a new instance of <see cref="ChatView"/> and initializes the component.
		/// </summary>
		public ChatView()
		{
			InitializeComponent();
		}
	}
}