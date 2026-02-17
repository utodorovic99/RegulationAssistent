namespace RegulationAssistantChatClient.ViewModels
{
	/// <summary>
	/// Represents a single chat message displayed in the UI.
	/// Instances are immutable and carry the message text and a flag indicating
	/// whether the message was sent by the user or the system/assistant.
	/// </summary>
	public class ChatMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMessage"/> class.
		/// </summary>
		/// <param name="content">The textual content of the chat message.</param>
		/// <param name="isUserMessage">True when the message was sent by the user; false for system or assistant messages.</param>
		public ChatMessage(string content, bool isUserMessage)
		{
			Content = content;
			IsUserMessage = isUserMessage;
		}

		/// <summary>
		/// The textual content of the message.
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Indicates whether this message was created by the user.
		/// Used by the UI to choose alignment, styles, or other presentation details.
		/// </summary>
		public bool IsUserMessage { get; }
	}
}