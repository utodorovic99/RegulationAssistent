using APIGatewayService.Context.Common;

namespace APIGatewayService.Common
{
	/// <summary>
	/// Processing result indicating only a status of the .
	/// </summary>
	internal class StatusProcessResult : IProcessingResult, IEquatable<StatusProcessResult>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StatusProcessResult"/>
		/// </summary>
		/// <param name="isSuccessful">Indicator whether processing is successfully executed.</param>
		private StatusProcessResult(bool isSuccessful)
		{
			IsSuccessful = isSuccessful;
		}

		/// <inheritdoc/>
		public bool IsSuccessful { get; init; }

		/// <summary>
		/// Cached successful processing result to avoid unnecessary allocations for common successful cases.
		/// </summary>
		public static StatusProcessResult Success { get; } = new StatusProcessResult(true);

		/// <summary>
		/// Cached failed processing result to avoid unnecessary allocations for common failure cases.
		/// </summary>
		public static StatusProcessResult Failed { get; } = new StatusProcessResult(false);

		/// <inheritdoc/>
		public bool Equals(StatusProcessResult? other)
		{
			return IsSuccessful == other?.IsSuccessful;
		}
	}
}