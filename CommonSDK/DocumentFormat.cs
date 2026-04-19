using System.Runtime.Serialization;

namespace CommonSDK
{
	public enum DocumentFormat : short
	{
		[EnumMember(Value = "docx")]
		Docx = 0,

		// future: Pdf, Txt, Odt etc.
	}
}