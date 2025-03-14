using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace LSAnalyzerAvalonia.Helper;

public static class ObjectTools
{
    // Kudos to Taras Alenin
    public static bool PublicInstancePropertiesEqual<T>(T self, T to, params string[] ignore) where T : class
    {
        if (self != null && to != null)
        {
            Type type = typeof(T);
            List<string> ignoreList = new List<string>(ignore);
            foreach (System.Reflection.PropertyInfo pi in type.GetProperties(System.Reflection.BindingFlags.Public |
                                                                             System.Reflection.BindingFlags.Instance))
            {
                if (!ignoreList.Contains(pi.Name))
                {
                    object? selfValue = type.GetProperty(pi.Name)!.GetValue(self, null);
                    object? toValue = type.GetProperty(pi.Name)!.GetValue(to, null);

                    if (selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return self == to;
    }

    // Kudos to Serj-Tm and Ian Kemp
    public static bool DoesPropertyExist(dynamic values, string name)
    {
        if (values is ExpandoObject)
            return ((IDictionary<string, object>)values).ContainsKey(name);

        return values.GetType().GetProperty(name) != null;
    }

    public static bool ElementObjectsEqual<T>(this ICollection<T> collection, ICollection<T> otherCollection,
        params string[] ignore) where T : class
    {
        if (collection.Count != otherCollection.Count)
        {
            return false;
        }

        for (int ii = 0; ii < collection.Count; ii++)
        {
            if (!ObjectTools.PublicInstancePropertiesEqual(collection.ElementAt(ii), otherCollection.ElementAt(ii),
                    ignore))
            {
                return false;
            }
        }

        return true;
    }
}