using Serilog.Events;

namespace Np.Logging.Logger.Enrichers
{
    internal class MaskFieldEnricher : FieldEnricherBase
    {
        private const string DefaultMask = "******";
        
        internal MaskFieldEnricher(IEnumerable<string> blacklistedNames) : base(blacklistedNames) { }

        protected override void InternalExecute(LogEvent logEvent, string name)
        {
            var maskedLogEvent = new LogEventProperty(name, new ScalarValue(DefaultMask));
            logEvent.AddOrUpdateProperty(maskedLogEvent);
        }

        protected override void InternalExecute(Dictionary<ScalarValue, LogEventPropertyValue> clonedChildElements, KeyValuePair<ScalarValue, LogEventPropertyValue> childElement)
        {
            clonedChildElements[childElement.Key] = new ScalarValue(DefaultMask);
        }

        protected override void InternalExecute(List<LogEventProperty> clonedChildProperties, LogEventProperty childProperty)
        {
            clonedChildProperties.Remove(childProperty);
            var maskedLogEvent = new LogEventProperty(childProperty.Name, new ScalarValue(DefaultMask));
            clonedChildProperties.Add(maskedLogEvent);
        }
    }
}