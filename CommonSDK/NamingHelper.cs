using System.Text.RegularExpressions;

namespace CommonSDK
{
	public static class NamingHelper
	{
		public static string SanitizeName(string input, int maxLength = 200)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return string.Empty;
			}

			string s = input.Trim();
			s = s.ToLowerInvariant();
			s = Regex.Replace(s, @"[^a-z0-9_\/\-]+", "-");
			s = Regex.Replace(s, "[-_]{2,}", "-");
			s = s.Trim('-', '_', '/');

			if (s.Length > maxLength)
			{
				s = s.Substring(0, maxLength);
			}

			return s;
		}

		public static string CreateVersionedName(string name, int version, bool sanitizeName = true)
		{
			string sanitizedName = sanitizeName
				? name
				: SanitizeName(name);

			return $"{sanitizedName}_v{version}";
		}
	}
}