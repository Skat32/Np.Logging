using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace Np.Logging.Logger.Helpers
{
    internal static class MaskingHelper
    {
        /// <summary>
        /// Compared with InvariantCultureIgnoreCase
        /// </summary>
        private static readonly ImmutableList<string> HeadersToMask = new List<string>
        {
            "Authorization"
        }.ToImmutableList();

        internal static IDictionary<string, string> MaskSecretHeaders(this IDictionary<string, string> dict) =>
            dict.ToDictionary(
                x => x.Key,
                x => HeadersToMask.Contains(x.Key, StringComparer.InvariantCultureIgnoreCase)
                    ? "******"
                    : x.Value);

        /// <summary>
        /// Supports only top-level JSON fields (nested not supported)
        /// </summary>
        private static readonly ImmutableList<string> JsonFieldsToMask = new List<string>
        {
            "access_token",
            "refresh_token",
            "client_secret",
            "password"
        }.ToImmutableList();

        internal static void MaskSecretsInJToken(JToken jToken)
        {
            var jPath = "$.['" + string.Join("', '", JsonFieldsToMask) + "']";

            var jTokensToMask = jToken.SelectTokens(jPath);
            foreach (var jTokenToMask in jTokensToMask) 
                ((JProperty) jTokenToMask.Parent!).Value = "******";
        }
    }
}
