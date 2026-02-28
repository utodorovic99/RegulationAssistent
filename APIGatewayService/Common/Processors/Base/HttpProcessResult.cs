using APIGatewayService.Context.Common;

namespace APIGatewayService.Common
{
	/// <summary>
	/// Processing result for a HTTP request.
	/// </summary>
	internal class HttpProcessResult : IProcessingResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HttpProcessResult"/>
		/// </summary>
		/// <param name="isSuccessful">Indicator whether processing is successfully executed.</param>
		private HttpProcessResult(bool isSuccessful)
		{
			IsSuccessful = isSuccessful;
		}

		/// <summary>
		/// Gets or sets indicator whether processing is successfully executed.
		/// </summary>
		public bool IsSuccessful { get; init; }

		/// <summary>
		/// Cached successful processing result to avoid unnecessary allocations for common successful cases.
		/// </summary>
		public static HttpProcessResult Success { get; } = new HttpProcessResult(true);

		/// <summary>
		/// Cached failed processing result to avoid unnecessary allocations for common failure cases.
		/// </summary>
		public static HttpProcessResult Failed { get; } = new HttpProcessResult(false);
	}
}