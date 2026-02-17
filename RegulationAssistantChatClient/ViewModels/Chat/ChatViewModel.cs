using ExternalServiceContracts.Common;
using ExternalServiceContracts.Requests;
using RegulationAssistantChatClient.Extensions;
using RegulationAssistantChatClient.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RegulationAssistantChatClient.ViewModels
{
	/// <summary>
	/// ViewModel for the chat UI. Manages messages, the user's question input, options such as date and organization type,
	/// and forwards queries to the regulation query service.
	/// </summary>
	public class ChatViewModel : INotifyPropertyChanged
	{
		private readonly RegulationQueryServiceProxy serviceProxy;
		private readonly OptionsViewModel options;
		private DateTime? selectedDate = DateTime.Now.Date;
		private string selectedOrganizationTypeString;
		private string question = string.Empty;

		/// <summary>
		/// Initializes a new instance of <see cref="ChatViewModel"/> using the provided <see cref="OptionsViewModel"/>.
		/// </summary>
		/// <param name="options">Options view model used to read user preferences such as selected language.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
		public ChatViewModel(OptionsViewModel options)
		{
			this.options = options ?? throw new ArgumentNullException(nameof(options));
			serviceProxy = new RegulationQueryServiceProxy(new System.Net.Http.HttpClient());
			SendCommand = new RelayCommand(async () => await SendQueryAsync(), CanSendQuery);
			ClearChatCommand = new RelayCommand(ClearChat);

			Language = this.options.Language;
			ResponseType = this.options.ResponseType;

			SelectedDate = DateTime.Now.Date;
			SelectedOrganizationTypeString = "Company";

			OrganizationTypes = new ObservableCollection<OrganizationTypeItem>();
			BuildOrganizationTypes();

			ChatMessages = new ObservableCollection<ChatMessage>
			{
				CreateWelcomeMessage(),
			};

			this.options.PropertyChanged += Options_PropertyChanged;
		}

		/// <summary>
		/// Event raised when a property value changes. Part of the INotifyPropertyChanged implementation to support data binding in the UI.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Collection of chat messages shown in the UI.
		/// </summary>
		public ObservableCollection<ChatMessage> ChatMessages { get; }

		/// <summary>
		/// The user's current question text.
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
		/// The date used as context for regulation queries. Bound to the DatePicker in the UI.
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
		/// The selected organization type (enum name as string). Bound to the ComboBox selected value.
		/// </summary>
		public string SelectedOrganizationTypeString
		{
			get
			{
				return selectedOrganizationTypeString;
			}

			set
			{
				selectedOrganizationTypeString = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Collection of localized organization type items shown in the ComboBox.
		/// </summary>
		public ObservableCollection<OrganizationTypeItem> OrganizationTypes { get; }

		/// <summary>
		/// The currently selected language name (e.g. "English" or "Serbian"). Read from the OptionsViewModel.
		/// </summary>
		public string Language { get; private set; }

		/// <summary>
		/// The user's selected response type label. Read from the OptionsViewModel.
		/// </summary>
		public string ResponseType { get; private set; }

		/// <summary>
		/// Command bound to the Send button. Executes sending the current question.
		/// </summary>
		public ICommand SendCommand { get; }

		/// <summary>
		/// Command bound to the Clear button. Clears the chat when executed.
		/// </summary>
		public ICommand ClearChatCommand { get; }

		/// <summary>
		/// Localized label for the Send button.
		/// </summary>
		public string SendButtonLabel
		{
			get
			{
				return GetResourceValue("SendButton", "Send", Language);
			}
		}

		/// <summary>
		/// Localized label for the Clear Chat button.
		/// </summary>
		public string ClearButtonLabel
		{
			get
			{
				return GetResourceValue("ClearButton", "Clear Chat", Language);
			}
		}

		/// <summary>
		/// Property changed notification helper. Raises the PropertyChanged event for the specified property name. When called without arguments, it uses the caller member name as the property name.
		/// </summary>
		/// <param name="propertyName"></param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Handler for property change events raised by the OptionsViewModel. When the language or response type changes, this method updates the corresponding properties in this view model and raises property changed notifications to update the UI. It also rebuilds localized resources such as organization type display values and button labels when the language changes.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private void Options_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(OptionsViewModel.Language))
			{
				Language = options.Language;

				OnPropertyChanged(nameof(Language));
				OnPropertyChanged(nameof(SendButtonLabel));
				OnPropertyChanged(nameof(ClearButtonLabel));

				if (ChatMessages.Count > 0)
				{
					ChatMessages[0] = CreateWelcomeMessage();
				}

				BuildOrganizationTypes();
			}
			else if (e.PropertyName == nameof(OptionsViewModel.ResponseType))
			{
				ResponseType = options.ResponseType;
				OnPropertyChanged(nameof(ResponseType));
			}
		}

		/// <summary>
		/// Helper that reads a string resource for the requested culture based on the language name.
		/// Falls back to Serbian and invariant culture if the requested key is not found.
		/// </summary>
		private static string GetResourceValue(string key, string fallback, string? language)
		{
			var rm = RegulationAssistantChatClient.Properties.Resources.ResourceManager;
			CultureInfo culture;
			if (!string.IsNullOrWhiteSpace(language))
			{
				switch (language)
				{
					case "English":
						culture = new CultureInfo("en");
						break;
					case "Serbian":
						culture = new CultureInfo("sr");
						break;
					default:
						culture = CultureInfo.CurrentUICulture ?? CultureInfo.InvariantCulture;
						break;
				}
			}
			else
			{
				culture = CultureInfo.CurrentUICulture ?? CultureInfo.InvariantCulture;
			}

			string? val = rm.GetString(key, culture);
			if (string.IsNullOrWhiteSpace(val))
			{
				try
				{
					var sr = new CultureInfo("sr");
					val = rm.GetString(key, sr);
				}
				catch { }
			}
			if (string.IsNullOrWhiteSpace(val))
			{
				val = rm.GetString(key, CultureInfo.InvariantCulture);
			}
			return string.IsNullOrWhiteSpace(val) ? fallback : val!;
		}

		/// <summary>
		/// Creates the initial welcome chat message using localized resources.
		/// </summary>
		private ChatMessage CreateWelcomeMessage()
		{
			string text = GetResourceValue("WelcomeMessage", "Welcome to the Regulation Assistant! How can I help you?", Language);
			return new ChatMessage(text, false);
		}

		/// <summary>
		/// BUilds the list of organization types with localized display values based on the current language. It also tries to preserve the currently selected organization type across rebuilds when possible.
		/// </summary>
		private void BuildOrganizationTypes()
		{
			var current = SelectedOrganizationTypeString;
			OrganizationTypes.Clear();
			OrganizationTypes.Add(new OrganizationTypeItem("Company", GetResourceValue("OrganizationType_Company", "Company", Language)));
			OrganizationTypes.Add(new OrganizationTypeItem("Government", GetResourceValue("OrganizationType_Government", "Government", Language)));
			OrganizationTypes.Add(new OrganizationTypeItem("NonProfit", GetResourceValue("OrganizationType_NonProfit", "Non-Profit", Language)));
			OrganizationTypes.Add(new OrganizationTypeItem("Educational", GetResourceValue("OrganizationType_Educational", "Educational", Language)));
			OrganizationTypes.Add(new OrganizationTypeItem("Healthcare", GetResourceValue("OrganizationType_Healthcare", "Healthcare", Language)));
			OrganizationTypes.Add(new OrganizationTypeItem("Other", GetResourceValue("OrganizationType_Other", "Other", Language)));

			if (!string.IsNullOrWhiteSpace(current))
			{
				SelectedOrganizationTypeString = current;
			}
			else if (OrganizationTypes.Count > 0 && string.IsNullOrWhiteSpace(SelectedOrganizationTypeString))
			{
				SelectedOrganizationTypeString = OrganizationTypes[0].Tag;
			}

			OnPropertyChanged(nameof(OrganizationTypes));
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

			OrganizationType orgType = OrganizationType.Company;
			try
			{
				if (!string.IsNullOrWhiteSpace(SelectedOrganizationTypeString))
				{
					orgType = (OrganizationType)Enum.Parse(typeof(OrganizationType), SelectedOrganizationTypeString);
				}
			}
			catch
			{
				orgType = OrganizationType.Company;
			}

			var request = new RegulationQueryRequest
			{
				Question = Question,
				Context = new RegulationQueryContext
				{
					Date = DateOnly.FromDateTime(SelectedDate ?? DateTime.Now.Date),
					OrganizationType = orgType
				},
				Preferences = new QueryPreferences
				{
					Language = LanguageExtensions.FromString(options.Language, CultureInfo.CurrentUICulture),
					AnswerStyle = AnswerStyleExtensions.FromString(options.ResponseType, CultureInfo.CurrentUICulture)
				}
			};

			RegulationQueryResponse response;
			try
			{
				response = await serviceProxy.SendRegulationQueryAsync(request);
			}
			catch
			{
				response = FailedResponsesCached.FailedRegulationQueryResponse;
			}

			ChatMessages.Add(new ChatMessage(response.AsChatbotResponseString(), false));
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