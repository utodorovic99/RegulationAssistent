using System.Globalization;
using System.Threading;
using System.Windows;

namespace RegulationAssistantChatClient
{
	/// <summary>
	/// Class representing the application entry point and global application-level logic.
	/// </summary>
	public partial class App : Application
	{
		/// <summary>
		/// Startup event handler for the application.
		/// </summary>
		/// <param name="e">Event.</param>
		protected override void OnStartup(StartupEventArgs e)
		{
			var culture = new CultureInfo("sr");
			RegulationAssistantChatClient.Properties.Resources.Culture = culture;

			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;

			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;

			base.OnStartup(e);
		}
	}
}