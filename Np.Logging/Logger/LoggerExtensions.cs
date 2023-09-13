using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Np.Logging.Logger.Helpers;
using Serilog;

namespace Np.Logging.Logger
{
    /// <summary>
    /// Serilog extensions
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Add object as JSON with masked secret fields
        /// </summary>
        public static ILogger ForContextJson(this ILogger logger, string fieldName, object data)
        {
            switch (data)
            {
                case null:
                    return logger.ForContext(fieldName, data);
                case Exception ex:
                    return logger.ForContext(fieldName, ex.ToString());
            }

            var serializationErrors = new List<string>();
            var logBody = "not a JSON body";
            
            var serializer = new JsonSerializer();
            serializer.Error += (sender, errorArgs) =>
            {
                serializationErrors.Add(errorArgs.ErrorContext.Error.Message);
                errorArgs.ErrorContext.Handled = true;
            }; 
            
            try
            {
                var jToken = JToken.FromObject(data, serializer);
                if (jToken != null)
                {
                    MaskingHelper.MaskSecretsInJToken(jToken);
                    logBody = jToken.ToString();
                }
            }
            catch (Exception e)
            {
                logBody = data is string dataAsString
                    ? $"Failed to convert string to JToken. Reason: {e.Message}. String: {dataAsString}"
                    : $"Failed to convert {data.GetType()} to JToken. Reason: {e.Message}";
            }
            
            var loggerWithContexts = serializationErrors.Any()
                ? logger.ForContext("SerializationErrors", JsonConvert.SerializeObject(serializationErrors))
                : logger;
            
            return loggerWithContexts.ForContext(fieldName, logBody);
        }
    }
}