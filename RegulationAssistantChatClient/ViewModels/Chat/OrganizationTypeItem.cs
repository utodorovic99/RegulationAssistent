namespace RegulationAssistantChatClient.ViewModels
{
	/// <summary>
	/// Represents a single organization type option for display in the ComboBox.
	/// </summary>
	public class OrganizationTypeItem
	{
		/// <summary>
		/// Initializes a new instance of <see cref="OrganizationTypeItem"/>.
		/// </summary>
		/// <param name="tag">The enum name value (e.g., "Company"). This is used as the SelectedValue.</param>
		/// <param name="display">The localized display text shown in the ComboBox.</param>
		public OrganizationTypeItem(string tag, string display)
		{
			Tag = tag;
			Display = display;
		}

		/// <summary>
		/// Gets enum name used as the SelectedValue for the ComboBox.
		/// </summary>
		public string Tag { get; }

		/// <summary>
		/// Gets localized display text.
		/// </summary>
		public string Display { get; set; }
	}
}