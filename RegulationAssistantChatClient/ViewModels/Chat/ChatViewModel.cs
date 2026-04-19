using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using RegulationAssistantChatClient.Services;

namespace RegulationAssistantChatClient.ViewModels
{
	/// <summary>
	/// ViewModel for the chat UI. Manages messages, the user's question input, options such as date and organization type,
	/// and forwards queries to the regulation query service.
	/// </summary>
	public class ChatViewModel : INotifyPropertyChanged
	{
		private readonly RegulationQueryServiceProxy serviceProxy;
		private DateTime? selectedDate = DateTime.Now.Date;
		private string question = string.Empty;

		/// <summary>
		/// Initializes a new instance of <see cref="ChatViewModel"/>.
		/// </summary>
		public ChatViewModel()
		{
			serviceProxy = new RegulationQueryServiceProxy(new System.Net.Http.HttpClient());
			SendCommand = new RelayCommand(async () => await SendQueryAsync(), CanSendQuery);
			ClearChatCommand = new RelayCommand(ClearChat);

			SelectedDate = DateTime.Now.Date;

			ChatMessages = new ObservableCollection<ChatMessage>
			{
				CreateWelcomeMessage(),
			};
		}

		/// <summary>
		/// Event raised when a property value changes. Part of the INotifyPropertyChanged implementation to support data binding in the UI.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets collection of chat messages shown in the UI.
		/// </summary>
		public ObservableCollection<ChatMessage> ChatMessages { get; }

		/// <summary>
		/// Gets or sets user's current question text.
		/// </summary>
		public string Question
		{
			get
			{
				return question;
			}

			set
			{
				question = value;
				OnPropertyChanged();
				((RelayCommand)SendCommand).RaiseCanExecuteChanged();
			}
		}

		/// <summary>
		/// Gets or set the date used as context for regulation queries. Bound to the DatePicker in the UI.
		/// </summary>
		public DateTime? SelectedDate
		{
			get
			{
				return selectedDate;
			}

			set
			{
				selectedDate = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets command bound to the Send button. Executes sending the current question.
		/// </summary>
		public ICommand SendCommand { get; }

		/// <summary>
		/// Gets command bound to the Clear button. Clears the chat when executed.
		/// </summary>
		public ICommand ClearChatCommand { get; }

		/// <summary>
		/// Gets
		/// lLocalized label for the Send button.
		/// </summary>
		public string SendButtonLabel => "Posalji";

		/// <summary>
		/// Gets localized label for the Clear Chat button.
		/// </summary>
		public string ClearButtonLabel => "Ocisti";

		/// <summary>
		/// Property changed notification helper. Raises the PropertyChanged event for the specified property name. When called without arguments, it uses the caller member name as the property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Creates the initial welcome chat message using localized resources.
		/// </summary>
		private ChatMessage CreateWelcomeMessage()
		{
			string text = "Dobrodosli u pravnog asistenta. Kako vam mogu pomoci?";
			return new ChatMessage(text, false);
		}

		/// <summary>
		/// Sends the current query to the Regulation Query service and appends the resulting response
		/// to the chat message collection. The user's question is also added before sending.
		/// </summary>
		private async Task SendQueryAsync()
		{
			if (!string.IsNullOrWhiteSpace(Question))
			{
				ChatMessages.Add(new ChatMessage(Question, true));
			}

			var request = new RegulationQueryRequest
			{
				Question = Question,
				Context = new RegulationQueryContext
				{
					Date = DateOnly.FromDateTime(SelectedDate ?? DateTime.Now.Date),
				},
			};

			RegulationResponse response;
			try
			{
				response = await serviceProxy.SendRegulationQueryAsync(request);
			}
			catch
			{
				response = RegulationResponse.FailedResponse;
			}

			ChatMessages.Add(new ChatMessage(response.Answer, false));
			Question = string.Empty;
		}

		/// <summary>
		/// Clears the chat history and restores the initial welcome message.
		/// </summary>
		private void ClearChat()
		{
			ChatMessages.Clear();
			ChatMessages.Add(CreateWelcomeMessage());
		}

		/// <summary>
		/// Determines whether the Send command can execute. Requires non-empty question text.
		/// </summary>
		private bool CanSendQuery()
		{
			return !string.IsNullOrWhiteSpace(Question);
		}
	}
}