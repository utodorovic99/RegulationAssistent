namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Represents a HTTP processor that can handle requests of a specific type and produce results of a specific type.
	/// </summary>
	internal interface IHttpRequestProcessor : IRequestProcessor
	{
		/// <summary>
		/// Gets a HTTP prefix associated with the processor.
		/// </summary>
		string HttpPrefix { get; }
	}
}