namespace MicrowaveNetworks.Touchstone.IO
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

    internal static class ControlKeywords
    {
        public static readonly string BeginInfo = "Begin Information".FormatKeyword();
        public static readonly string EndInfo = "End Information".FormatKeyword();
        public static readonly string NetworkData = "Network Data".FormatKeyword();
        public static readonly string NoiseData = "Noise Data".FormatKeyword();
        public static readonly string End = "End".FormatKeyword();

        public static readonly TouchstoneKeyword BeginInfo;
    }
}
