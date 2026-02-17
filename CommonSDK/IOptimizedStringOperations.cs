using System.Text;

namespace CommonSDK
{
	/// <summary>
	/// Contains interface for optimized string operations.
	/// </summary>
	public interface IOptimizedStringOperations
	{
		/// <summary>
		/// Appends own string representation to an existing <paramref name="sb"/>.
		/// </summary>
		/// <param name="sb">String builder.</param>
		void AppendSelfAsString(StringBuilder sb);
	}
}