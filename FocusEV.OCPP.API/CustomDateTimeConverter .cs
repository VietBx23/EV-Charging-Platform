using System.Text.Json;
using System.Text.Json.Serialization;

namespace FocusEV.OCPP.API
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string format;

        public CustomDateTimeConverter(string format)
        {
            this.format = format;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString(), format, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(format));
        }
    }
}
