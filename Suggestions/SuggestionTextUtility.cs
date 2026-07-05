using System.Text.RegularExpressions;

namespace ChatUtilities.Suggestions
{
    public class SuggestionTextUtility
    {
        public static bool TryParsePositiveInteger(string text, out int number)
        {
            number = 0;

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                {
                    return false;
                }
            }

            if (!int.TryParse(text, out number))
            {
                number = 0;
                return false;
            }

            if (number <= 0)
            {
                number = 0;
                return false;
            }

            return true;
        }

        public static string RemovePlaceholders(string text)
        {
            string safeText = text ?? string.Empty;
            bool hadPlaceholder = safeText.Contains("[");
            string cleanedText = Regex.Replace(safeText, "\\[[^\\]]*\\]", string.Empty).TrimEnd();

            if (hadPlaceholder)
            {
                cleanedText += " ";
            }

            return cleanedText;
        }

        public static bool ContainsWhiteSpace(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
