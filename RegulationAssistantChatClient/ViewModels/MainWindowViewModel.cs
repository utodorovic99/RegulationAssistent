using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace RegulationAssistantChatClient.ViewModels
{
	/// <summary>
	/// View model that composes the application's main child view models and forwards selected properties and commands for use by the view's XAML bindings.
	/// </summary>
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of <see cref="MainWindowViewModel"/> and wires child view model property change
		/// notifications so bindings against this view model remain up-to-date.
		/// </summary>
		public MainWindowViewModel()
		{
			ChatViewModel = new ChatViewModel();

			ChatViewModel.PropertyChanged += Child_PropertyChanged;
		}

		/// <summary>
		/// Event raised when a property on this view model changes. This event is also raised when child view models
		/// raise property change notifications and are forwarded by this view model.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets the chat view model which manages chat messages, question input and query-related state.
		/// </summary>
		public ChatViewModel ChatViewModel { get; }

		/// <summary>
		/// Gets the collection of chat messages exposed by the <see cref="ChatViewModel"/>.
		/// This property is forwarded to preserve existing bindings that reference <c>ChatMessages</c> on the main VM.
		/// </summary>
		public ObservableCollection<ChatMessage> ChatMessages
		{
			get
			{
				return ChatViewModel.ChatMessages;
			}
		}

		/// <summary>
		/// Gets or sets the current question text through the <see cref="ChatViewModel"/>.
		/// Setting this property forwards the value to the child view model.
		/// </summary>
		public string Question
		{
			get
			{
				return ChatViewModel.Question;
			}

			set
			{
				ChatViewModel.Question = value;
			}
		}

		/// <summary>
		/// Gets the command that sends the current question. This is forwarded from <see cref="ChatViewModel"/>.
		/// </summary>
		public ICommand SendCommand
		{
			get
			{
				return ChatViewModel.SendCommand;
			}
		}

		/// <summary>
		/// Gets the command that clears the chat history. This is forwarded from <see cref="ChatViewModel"/>.
		/// </summary>
		public ICommand ClearChatCommand
		{
			get
			{
				return ChatViewModel.ClearChatCommand;
			}
		}

		/// <summary>
		/// Gets the command that traces last request. This is forwarded from <see cref="ChatViewModel"/>.
		/// </summary>
		public ICommand TraceLastRequestCommand
		{
			get
			{
				return ChatViewModel.TraceLastRequestCommand;
			}
		}

		/// <summary>
		/// Gets or sets the selected date used for regulation queries. This is forwarded to the <see cref="ChatViewModel"/>.
		/// </summary>
		public DateTime? SelectedDate
		{
			get
			{
				return ChatViewModel.SelectedDate;
			}

			set
			{
				ChatViewModel.SelectedDate = value;
			}
		}

		/// <summary>
		/// Handler for property change events raised by child view models. Re-raises the event from this view model
		/// using the same property name so XAML bindings bound to <see cref="MainWindowViewModel"/> are notified.
		/// </summary>
		/// <param name="sender">The child view model that raised the event.</param>
		/// <param name="e">Property changed event arguments containing the property name.</param>
		private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));
		}
	}
}