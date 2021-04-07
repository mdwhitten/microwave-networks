using System;
using System.Collections.Generic;
using System.Reflection;

namespace Touchstone.IO
{
    /// <summary>
    /// Simple helper class to map between the text values from the Touchstone file and the enum values defined by reading the
    /// <see cref="TouchstoneParameterAttribute"/> if it exists.
    /// <para></para>
    /// Implemented as a static class so as to not need to invoke reflection more than once.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class TouchstoneEnumMap<T> where T : Enum
    {
        private static Dictionary<T, string> fieldNameLookup;
        private static Dictionary<string, T> valueLookup;

        static TouchstoneEnumMap()
        {
            fieldNameLookup = new Dictionary<T, string>();
            valueLookup = new Dictionary<string, T>();

            Array values = Enum.GetValues(typeof(T));
            string[] names = Enum.GetNames(typeof(T));

            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = typeof(T).GetField(names[i]);
                string fieldName;
                if (field.IsDefined(typeof(TouchstoneParameterAttribute)))
                {
                    var attr = field.GetCustomAttribute<TouchstoneParameterAttribute>();
                    fieldName = attr.FieldName;
                }
                else fieldName = names[i];

                T value = (T)values.GetValue(i);
                fieldNameLookup.Add(value, fieldName);
                valueLookup.Add(fieldName, value);

            }
        }

        /// <summary>
        /// Returns whether <paramref name="name"/> is a valid Touchstone entry for the parameter type specified
        /// by <typeparamref name="T"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool ValidTouchstoneName(string name) => valueLookup.ContainsKey(name);
        /// <summary>
        /// Returns the appropriate enum type <typeparamref name="T"/> matched to the <paramref name="name"/> value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T FromTouchstoneValue(string name) => valueLookup[name];
        /// <summary>
        ///  Returns the value of <see cref="TouchstoneParameterAttribute.FieldName"/> if <paramref name="value"/> has
        ///  this attribute. Otherwise, simply returns the name of <paramref name="value"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToTouchstoneValue(T value) => fieldNameLookup[value];
    }
}
