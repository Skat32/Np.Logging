using Newtonsoft.Json;
using Np.Logging.Logger.Converters;
using Serilog.Events;

namespace Np.Logging.Logger.Http.Models
{
    /// <summary>
    /// Logging endpoint request params
    /// </summary>
    public class LoggingEndpointParams
    {
        /// <summary>
        /// Min loglevel for whole application
        /// </summary>
        [JsonConverter(typeof(CustomStringEnumConverter))]
        public LogEventLevel Loglevel { get; set; }
    }
}