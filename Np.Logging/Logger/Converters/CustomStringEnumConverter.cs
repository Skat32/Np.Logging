using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog.Events;

namespace Np.Logging.Logger.Converters
{
    /// <summary>
    /// Custom (de)serializer for invalidating not defined Enums
    /// </summary>
    internal class CustomStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string)reader.Value!;
            var isSuccess =
                Enum.TryParse(value, true, out LogEventLevel logLevel)
                && Enum.IsDefined(typeof(LogEventLevel), logLevel); // because Enum.Parse<EType>("number as string") is always valid TEnum, even if not defined explicitly

            if (!isSuccess)
                throw new SerializationException($"Could not convert base class {objectType}");
            
            return logLevel; 
        }
    }
}