using RegulationAssistantChatClient.ViewModels;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace RegulationAssistantChatClient.Views
{
	/// <summary>
	/// Interaction logic for the options view user control. Hosts UI for selecting application options
	/// such as language and answer style and applies the selected culture to the application's resources.
	/// </summary>
	public partial class OptionsView : UserControl
	{
		/// <summary>
		/// Initializes a new instance of <see cref="OptionsView"/> and attaches the Loaded event handler.
		/// </summary>
		public OptionsView()
		{
			InitializeComponent();
			this.Loaded += OptionsView_Loaded;
		}

		/// <summary>
		/// Handles the control's Loaded event. Wires the view model's PropertyChanged event and ensures
		/// the resource culture is initialized to the current language selection.
		/// </summary>
		/// <param name="sender">Event sender (the OptionsView).</param>
		/// <param name="e">Routed event arguments.</param>
		private void OptionsView_Loaded(object? sender, RoutedEventArgs e)
		{
			if (this.DataContext is OptionsViewModel vm)
			{
				vm.PropertyChanged += Vm_PropertyChanged;
				SetResourcesCulture(vm.Language);
			}
		}

		/// <summary>
		/// Handles property change notifications coming from the bound <see cref="OptionsViewModel"/>.
		/// When the language changes this method updates the application's resource culture accordingly.
		/// </summary>
		/// <param name="sender">The view model that raised the event.</param>
		/// <param name="e">Property changed event arguments.</param>
		private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(OptionsViewModel.Language) && this.DataContext is OptionsViewModel vm)
			{
				SetResourcesCulture(vm.Language);
			}
		}

		/// <summary>
		/// Applies the requested language selection to the application's resource manager and thread culture settings.
		/// Supports an explicit Serbian culture selection; all other values use the invariant culture.
		/// </summary>
		/// <param name="language">The language name selected by the user (e.g. "Serbian").</param>
		private void SetResourcesCulture(string language)
		{
			CultureInfo culture;
			if (string.Equals(language, "Serbian", System.StringComparison.OrdinalIgnoreCase))
			{
				culture = new CultureInfo("sr");
			}
			else
			{
				culture = CultureInfo.InvariantCulture;
			}

			RegulationAssistantChatClient.Properties.Resources.Culture = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
		}
	}
}