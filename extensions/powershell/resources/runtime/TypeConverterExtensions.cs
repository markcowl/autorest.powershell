using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Rest.ClientRuntime.PowerShell
{
    internal static class TypeConverterExtensions
    {

        internal static System.Collections.Generic.IEnumerable<object> GetPropertyKeys<K, V>(this System.Collections.Generic.IDictionary<K, V> dictionary)
        {
            if (null != dictionary)
            {
                foreach (var each in dictionary.Keys)
                {
                    yield return each;
                }
            }
        }
        internal static System.Collections.Generic.IEnumerable<object> GetPropertyKeys(this System.Collections.IDictionary dictionary)
        {
            if (null != dictionary)
            {
                foreach (var each in dictionary.Keys)
                {
                    yield return each;
                }
            }
        }
        internal static System.Collections.Generic.IEnumerable<object> GetPropertyKeys(this System.Management.Automation.PSObject instance)
        {
            if (null != instance)
            {
                foreach (var each in instance.Properties)
                {
                    yield return each;
                }
            }
        }

        internal static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>> GetFilteredProperties<K, V>(this System.Collections.Generic.IDictionary<K, V> instance, global::System.Collections.Generic.HashSet<string> exclusions = null, global::System.Collections.Generic.HashSet<string> inclusions = null)
        {
            return (null == instance || instance.Count == 0) ?
                 Enumerable.Empty<System.Collections.Generic.KeyValuePair<K, V>>() :
                 instance.Keys
                    .Where(key =>
                       !(true == exclusions?.Contains(key?.ToString()))
                       && (false != inclusions?.Contains(key?.ToString())))
                    .Select(key => new System.Collections.Generic.KeyValuePair<K, V>(key, instance[key]));
        }

        internal static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object, object>> GetFilteredProperties(this System.Collections.IDictionary instance, global::System.Collections.Generic.HashSet<string> exclusions = null, global::System.Collections.Generic.HashSet<string> inclusions = null)
        {
            return (null == instance || instance.Count == 0) ?
                 Enumerable.Empty<System.Collections.Generic.KeyValuePair<object, object>>() :
                 instance.Keys.OfType<object>()
                    .Where(key =>
                       !(true == exclusions?.Contains(key?.ToString()))
                       && (false != inclusions?.Contains(key?.ToString())))
                    .Select(key => new System.Collections.Generic.KeyValuePair<object, object>(key, instance[key]));
        }

        internal static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object, object>> GetFilteredProperties(this System.Management.Automation.PSObject instance, global::System.Collections.Generic.HashSet<string> exclusions = null, global::System.Collections.Generic.HashSet<string> inclusions = null)
        {
            // new global::System.Collections.Generic.HashSet<string>(System.StringComparer.InvariantCultureIgnoreCase)
            return (null == instance || !instance.Properties.Any()) ?
                 Enumerable.Empty<System.Collections.Generic.KeyValuePair<object, object>>() :
                 instance.Properties
                    .Where(property =>
                       !(true == exclusions?.Contains(property.Name))
                       && (false != inclusions?.Contains(property.Name)))
                    .Select(property => new System.Collections.Generic.KeyValuePair<object, object>(property.Name, property.Value));
        }


        internal static T GetValueForProperty<T, K, V>(this System.Collections.Generic.IDictionary<K, V> dictionary, string propertyName, T defaultValue, System.Func<object, object> converter)
        {
            try
            {
                var key = System.Linq.Enumerable.FirstOrDefault(dictionary.Keys, each => System.String.Equals(each.ToString(), propertyName, System.StringComparison.CurrentCultureIgnoreCase));
                return key == null ? defaultValue : (T)converter(dictionary[key]);
            }
            catch
            {
            }
            return defaultValue;
        }
        internal static T GetValueForProperty<T>(this System.Collections.IDictionary dictionary, string propertyName, T defaultValue, System.Func<object, object> converter)
        {
            try
            {
                var key = System.Linq.Enumerable.FirstOrDefault(dictionary.Keys.OfType<object>(), each => System.String.Equals(each.ToString(), propertyName, System.StringComparison.CurrentCultureIgnoreCase));
                return key == null ? defaultValue : (T)converter(dictionary[key]);
            }
            catch
            {
            }
            return defaultValue;
        }

        internal static T GetValueForProperty<T>(this System.Management.Automation.PSObject psObject, string propertyName, T defaultValue, System.Func<object, object> converter)
        {
            try
            {
                var property = System.Linq.Enumerable.FirstOrDefault(psObject.Properties, each => System.String.Equals(each.Name.ToString(), propertyName, System.StringComparison.CurrentCultureIgnoreCase));
                return property == null ? defaultValue : (T)converter(property.Value);
            }
            catch
            {
            }
            return defaultValue;
        }
    }
}
