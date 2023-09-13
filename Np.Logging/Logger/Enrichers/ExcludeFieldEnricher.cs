using Serilog.Events;

namespace Np.Logging.Logger.Enrichers
{
    internal class ExcludeFieldEnricher : FieldEnricherBase
    {
        internal ExcludeFieldEnricher(IEnumerable<string> blacklistedNames) : base(blacklistedNames)
        {
        }

        protected override void InternalExecute(LogEvent logEvent, string name)
        { 
            logEvent.RemovePropertyIfPresent(name);
        }

        protected override void InternalExecute(
            Dictionary<ScalarValue, LogEventPropertyValue> clonedChildElements,
            KeyValuePair<ScalarValue, LogEventPropertyValue> childElement)
        {
            clonedChildElements.Remove(childElement.Key);
        }

        protected override void InternalExecute(List<LogEventProperty> clonedChildProperties, LogEventProperty childProperty)
        {
            clonedChildProperties.Remove(childProperty);
        }
    }
}