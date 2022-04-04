using System;
using System.Collections.Generic;
using System.Text;

namespace MicrowaveNetworks.Touchstone.Internal
{


    using IO;
    internal class TouchstoneKeywordFormatter
    {
        internal string KeywordText { get; set; }
       /* public virtual T ParseKeyword<T>(string keyword)
        {

        }*/
        public virtual string FormatFullKeyword<T>(string keyword, T value)
        {
            return keyword.FormatKeyword() + " " + value.ToString();
        }
    }
    /*
    public abstract class TouchstoneKeyword
    {
        internal abstract string KeywordText { get; }

        public override string ToString() => KeywordText.FormatKeyword();

        public virtual bool ValidKeyword(string textToValidate)
        {
            textToValidate = textToValidate?.Trim();
            return string.Equals(textToValidate, ToString());
        }
    }
    public struct TouchstoneKeyword<T> : TouchstoneKeyword
    {
        bool _hasValue;
        T _value;

        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    throw new InvalidOperationException("The keyword value has not been set.");
                }
                else return _value;
            }
            set
            {
                _hasValue = true;
                _value = value;
            }
        }
        public bool HasValue => _hasValue;
        
        internal override string KeywordText => throw new InvalidOperationException("This is only valid in dervied classes");

        private TouchstoneKeyword() { }

        public static implicit operator TouchstoneKeyword<T> (T value) => new TouchstoneKeyword<T>() { Value = value };
        public static explicit operator T (TouchstoneKeyword<T> keyword) => keyword.Value;

        public override string ToString()
        {
            return base.ToString() + " " + Value.ToString();
        }
        public static virtual FromString
    }*/
}
