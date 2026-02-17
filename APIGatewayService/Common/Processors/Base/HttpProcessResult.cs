using APIGatewayService.Context.Common;

namespace APIGatewayService.Common
{
	internal class HttpProcessResult : IProcessingResult
	{
		private HttpProcessResult(bool isSuccessful)
		{
			IsSuccessful = isSuccessful;
		}

		public bool IsSuccessful { get; init; }

		public static HttpProcessResult Success { get; } = new HttpProcessResult(true);

		public static HttpProcessResult Failed { get; } = new HttpProcessResult(false);
	}
}