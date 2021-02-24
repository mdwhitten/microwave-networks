using System;
using System.Collections.Generic;
using System.Reflection;

namespace Touchstone.IO
{
    internal class FieldLookup<T>
    {
        private Lazy<Dictionary<string, string>> fieldLookup;

        public Dictionary<string, string> Value => fieldLookup.Value;

        public FieldLookup()
        {
            fieldLookup = new Lazy<Dictionary<string, string>>(() => CreateFieldLookup());
        }

        private static Dictionary<string, string> CreateFieldLookup()
        {
            var keywordLookup = new Dictionary<string, string>();
            var fields = typeof(T).GetFields();
            foreach (var field in fields)
            {
                string fieldName;
                if (field.IsDefined(typeof(TouchstoneParameterAttribute)))
                {
                    var attr = field.GetCustomAttribute<TouchstoneParameterAttribute>();
                    fieldName = attr.FieldName;
                }
                else fieldName = field.Name;

                keywordLookup.Add(fieldName, field.Name);
            }
            return keywordLookup;
        }
    }
}
