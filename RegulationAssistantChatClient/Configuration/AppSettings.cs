namespace RegulationAssistantChatClient.Configuration
{
	/// <summary>
	/// Configuration container for application settings consumed by the chat client.
	/// Instances of this class are immutable after creation (init-only properties) and provide
	/// a central place to read configurable values such as service endpoints.
	/// </summary>
	public sealed class AppSettings
	{
		/// <summary>
		/// Base URL of the Regulation Query service used by the chat client. Defaults to
		/// "http://localhost:8080/RegulationQuery" when not provided in configuration.
		/// </summary>
		public string RegulationQueryServiceBaseUrl { get; set; } = "http://localhost:8080/RegulationQuery";

		/// <summary>
		/// Base URL of the Document Storage service used by the chat client. Defaults to
		/// "http://localhost:8080/Documents" when not provided in configuration.
		/// </summary>
		public string DocumentStorageServiceBaseUrl { get; set; } = "http://localhost:8080/Documents";

		/// <summary>
		/// Base URL of the auditing service used by the chat client. Defaults to
		/// "http://localhost:8080/Tracing"" when not provided in configuration.
		/// </summary>
		public string AuditingServiceBaseUrl { get; set; } = "http://localhost:8080/Auditing";
	}
}