using Serilog.Core;
using Serilog.Events;

namespace Np.Logging.Logger.Enrichers
{
    internal abstract class FieldEnricherBase : ILogEventEnricher
    {
        private readonly IEnumerable<string> _blacklistedNames;

        private bool IsBlacklistedName(string name)
            => _blacklistedNames.Any(bl => bl.Equals(name, StringComparison.OrdinalIgnoreCase));

        internal FieldEnricherBase(IEnumerable<string> blacklistedNames)
        {
            _blacklistedNames = blacklistedNames ?? throw new ArgumentException(nameof(blacklistedNames));
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var (name, value) in logEvent.Properties)
            {
                if (IsBlacklistedName(name))
                    InternalExecute(logEvent, name);
                else
                {
                    var updatedPropertyValue = Apply(value, name);
                    
                    if (updatedPropertyValue != null)
                        logEvent.AddOrUpdateProperty(new LogEventProperty(name, updatedPropertyValue));
                }
            }
        }

        protected abstract void InternalExecute(LogEvent logEvent, string name);

        private LogEventPropertyValue? Apply(SequenceValue value, string parentName)
        {
            var childElements = value.Elements.ToList();
            var clonedChildElements = new List<LogEventPropertyValue>(childElements);

            var requireUpdate = false;
            
            foreach (var childElement in childElements)
            {
                var updatedChild = Apply(childElement, parentName);
                
                if (updatedChild == null) continue;
                
                clonedChildElements[clonedChildElements.FindIndex(x => x == childElement)] = updatedChild;
                requireUpdate = true;
            }

            return requireUpdate ? new SequenceValue(clonedChildElements) : null;
        }

        private LogEventPropertyValue? Apply(DictionaryValue value, string parentName)
        {
            var childElements = value.Elements.ToList();
            var clonedChildElements = value.Elements.ToDictionary(x => x.Key, x => x.Value);

            var requireUpdate = false;
            
            foreach (var childElement in childElements)
            {
                var name = $"{parentName}.{childElement.Key.Value}";
                
                if (IsBlacklistedName(name))
                {
                    InternalExecute(clonedChildElements, childElement);
                    requireUpdate = true;
                }
                else
                {
                    var updatedChild = Apply(childElement.Value, name);
                    
                    if (updatedChild == null) continue;
                    
                    clonedChildElements[childElement.Key] = updatedChild;
                    requireUpdate = true;
                }
            }

            return requireUpdate ? new DictionaryValue(clonedChildElements) : null;
        }

        protected abstract void InternalExecute(Dictionary<ScalarValue, LogEventPropertyValue> clonedChildElements,
            KeyValuePair<ScalarValue, LogEventPropertyValue> childElement);

        private LogEventPropertyValue? Apply(StructureValue value, string parentName)
        {
            var childProperties = value.Properties.ToList();
            var clonedChildProperties = new List<LogEventProperty>(childProperties);

            var requireUpdate = false;
            
            foreach (var childProperty in childProperties)
            {
                var name = $"{parentName}.{childProperty.Name}";
                
                if (IsBlacklistedName(name))
                {
                    InternalExecute(clonedChildProperties, childProperty);
                    requireUpdate = true;
                }
                else
                {
                    var updatedChild = Apply(childProperty.Value, name);
                    
                    if (updatedChild == null) continue;
                    
                    clonedChildProperties[clonedChildProperties.FindIndex(x => x == childProperty)]
                        = new LogEventProperty(childProperty.Name, updatedChild);
                    requireUpdate = true;
                }
            }

            return requireUpdate ? new StructureValue(clonedChildProperties) : null;
        }

        protected abstract void InternalExecute(List<LogEventProperty> clonedChildProperties,
            LogEventProperty childProperty);

        private LogEventPropertyValue? Apply(LogEventPropertyValue value, string parentName) =>
            value switch
            {
                SequenceValue sequenceValue => Apply(sequenceValue, parentName),
                DictionaryValue dictionaryValue => Apply(dictionaryValue, parentName),
                StructureValue structureValue => Apply(structureValue, parentName),
                _ => null
            };
    }
}