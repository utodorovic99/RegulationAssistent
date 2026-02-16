namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Supported language codes used for query preferences.
	/// Serialized/deserialized as a JSON string (e.g. "en", "RS") when converters are configured.
	/// </summary>
	public enum SupportedLanguage
	{
		En,
		RS,
	}
}
