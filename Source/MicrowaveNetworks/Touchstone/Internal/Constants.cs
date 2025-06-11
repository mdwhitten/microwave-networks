namespace MicrowaveNetworks.Touchstone.Internal
{
    internal static class Constants
    {
        public const char CommentChar = '!';
        public const char OptionChar = '#';
        public const char KeywordOpenChar = '[';
        public const char KeywordCloseChar = ']';
        public const char ResistanceChar = 'R';

        public static string FormatKeyword(this string keywordText)
            => KeywordOpenChar + keywordText + KeywordCloseChar;
    }

}
