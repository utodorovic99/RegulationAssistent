using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RegulationAssistantChatClient.ViewModels
{
	/// <summary>
	/// View model containing user-selectable application options such as language and answer style.
	/// Raises <see cref="INotifyPropertyChanged"/> when properties change so the UI can react.
	/// </summary>
	public class OptionsViewModel : INotifyPropertyChanged
	{
		private string language = "Serbian";
		private string responseType = "Concise with Citations";

		/// <summary>
		/// Event raised when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Currently selected language name displayed in the UI and used for localization decisions.
		/// Default is "Serbian".
		/// </summary>
		public string Language
		{
			get
			{
				return language;
			}

			set
			{
				if (language == value) return;
				language = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Selected answer style (e.g. "Concise with Citations").
		/// </summary>
		public string ResponseType
		{
			get
			{
				return responseType;
			}

			set
			{
				if (responseType == value)
				{
					return;
				}

				responseType = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Raises the <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="propertyName">Name of property that changed (inferred from caller when omitted).</param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}