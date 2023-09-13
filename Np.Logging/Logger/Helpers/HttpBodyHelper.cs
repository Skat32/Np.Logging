using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Serilog;

[assembly: InternalsVisibleTo("Np.Logging.UnitTests")]
namespace Np.Logging.Logger.Helpers
{
    internal static class HttpBodyHelper
    {
        internal static dynamic Destruct(string contentType, string requestBody, ILogger logger)
        {
            try
            {
                if (IsJsonContentType(contentType))
                    return JsonConvert.DeserializeObject<dynamic>(requestBody);

                if (IsFormUrlEncoded(contentType))
                {
                    var parsedParams = HttpUtility.ParseQueryString(requestBody);
                    return parsedParams.AllKeys.ToDictionary(x => x!, x => parsedParams[x]);
                }

                if (IsMultipartFormEncoded(contentType))
                    return new {MultipartBodyAsText = requestBody};
            }
            catch (Exception ex)
            {
                logger
                    .ForContext("Exception", ex, true)
                    .ForContext("ContentType", contentType)
                    .ForContext("RequestBody", requestBody)
                    .Warning("Body deserialization error");
            }

            return new { BodyAsText = requestBody};
        }

        internal static bool IsJsonContentType(string contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                   && contentType.StartsWith("application/", StringComparison.OrdinalIgnoreCase)
                   && contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) != -1;
        }

        internal  static bool IsFormUrlEncoded(string contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                   && contentType.IndexOf("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) != -1;
        }
        
        internal  static bool IsMultipartFormEncoded(string contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                   && contentType.StartsWith("multipart", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsOctetStream(string contentType)
        {
            return string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool HasValidMultipartBoundary(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeaderValue.Boundary);
            
            return !string.IsNullOrWhiteSpace(boundary.Value);
        }
    }
}