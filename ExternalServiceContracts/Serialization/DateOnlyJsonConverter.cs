using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExternalServiceContracts.Serialization
{
	/// <summary>
	/// Factory that provides JSON converters for DateOnly and nullable DateOnly using a consistent "yyyy-MM-dd" format.
	/// </summary>
	public sealed class DateOnlyJsonConverterFactory : JsonConverterFactory
	{
		private const string DateFormat = "yyyy-MM-dd";

		public override bool CanConvert(Type typeToConvert)
		{
			if (typeToConvert == typeof(DateOnly)) return true;
			if (typeToConvert == typeof(DateOnly?)) return true;
			return false;
		}

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			if (typeToConvert == typeof(DateOnly))
			{
				return new DateOnlyJsonConverter(DateFormat);
			}

			if (typeToConvert == typeof(DateOnly?))
			{
				return new NullableDateOnlyJsonConverter(DateFormat);
			}

			throw new NotSupportedException($"Cannot create converter for {typeToConvert}.");
		}
	}

	internal sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
	{
		private readonly string format;

		public DateOnlyJsonConverter(string format) => this.format = format;

		public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				string? s = reader.GetString();
				if (string.IsNullOrEmpty(s)) throw new JsonException("Invalid date format for DateOnly.");
				return DateOnly.ParseExact(s, format);
			}

			throw new JsonException("Expected string token when parsing DateOnly.");
		}

		public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString(format));
		}
	}

	internal sealed class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
	{
		private readonly string format;

		public NullableDateOnlyJsonConverter(string format) => this.format = format;

		public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null) return null;
			if (reader.TokenType == JsonTokenType.String)
			{
				string? s = reader.GetString();
				if (string.IsNullOrEmpty(s)) return null;
				return DateOnly.ParseExact(s, format);
			}

			throw new JsonException("Expected string or null token when parsing DateOnly?.");
		}

		public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
		{
			if (value.HasValue)
			{
				writer.WriteStringValue(value.Value.ToString(format));
			}
			else
			{
				writer.WriteNullValue();
			}
		}
	}
}