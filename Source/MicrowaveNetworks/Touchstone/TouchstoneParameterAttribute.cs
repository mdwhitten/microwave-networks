using System;

namespace MicrowaveNetworks.Touchstone
{
    internal class TouchstoneParameterAttribute : Attribute
    {
        public string FieldName { get; }
        public TouchstoneParameterAttribute(string fieldName) => FieldName = fieldName;
    }

    internal sealed class TouchstoneKeywordAttribute : TouchstoneParameterAttribute
    {
        TouchstoneKeywordFormatter formatter;

        public TouchstoneKeywordAttribute(string fieldName)
            : base(fieldName)
        {
            formatter = new TouchstoneKeywordFormatter() { KeywordText = fieldName };
        }
        public TouchstoneKeywordAttribute(string fieldName, TouchstoneKeywordFormatter formatter)
            : base(fieldName)
        {
            formatter.KeywordText = fieldName;
            this.formatter = formatter;
        }
    }
}
