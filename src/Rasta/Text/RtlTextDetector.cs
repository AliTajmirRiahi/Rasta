namespace Rasta.Text
{
    /// <summary>
    /// Detects Persian/Arabic (right-to-left) text and resolves its base direction.
    /// Pure logic with no Visual Studio dependencies.
    /// </summary>
    public static class RtlTextDetector
    {
        /// <summary>
        /// Returns <c>true</c> when <paramref name="c"/> lies in one of the Arabic-script blocks
        /// (Arabic, Arabic Supplement, Arabic Extended-A, Presentation Forms A and B).
        /// </summary>
        /// <param name="c">The character to test.</param>
        public static bool IsRtlCharacter(char c)
        {
            return (c >= '؀' && c <= 'ۿ')
                || (c >= 'ݐ' && c <= 'ݿ')
                || (c >= 'ࢠ' && c <= 'ࣿ')
                || (c >= 'ﭐ' && c <= '﷿')
                || (c >= 'ﹰ' && c <= '﻿');
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="c"/> is an Arabic-script character that is a letter.
        /// Digits, punctuation and combining marks do not count.
        /// </summary>
        /// <param name="c">The character to test.</param>
        public static bool IsRtlLetter(char c)
        {
            return IsRtlCharacter(c) && char.IsLetter(c);
        }

        /// <summary>Counts the Arabic-script letters in <paramref name="text"/>.</summary>
        /// <param name="text">The text to scan; may be <c>null</c>.</param>
        public static int CountRtlLetters(string? text)
        {
            if (text == null || text.Length == 0)
            {
                return 0;
            }

            int count = 0;
            foreach (char c in text)
            {
                if (IsRtlLetter(c))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="text"/> contains at least
        /// <paramref name="minimumRtlLetters"/> Arabic-script letters (a value below 1 is treated as 1).
        /// </summary>
        /// <param name="text">The text to scan; may be <c>null</c>.</param>
        /// <param name="minimumRtlLetters">The required number of RTL letters.</param>
        public static bool ContainsRtl(string? text, int minimumRtlLetters)
        {
            int required = minimumRtlLetters < 1 ? 1 : minimumRtlLetters;
            return CountRtlLetters(text) >= required;
        }

        /// <summary>
        /// Resolves the base direction of <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text to analyse; may be <c>null</c>.</param>
        /// <param name="forceRtlWhenPersian">
        /// When <c>true</c>, any text containing an RTL letter is right-to-left; otherwise the first
        /// strong directional letter decides.
        /// </param>
        /// <returns>
        /// <see cref="TextDirection.RightToLeft"/> or <see cref="TextDirection.LeftToRight"/>
        /// (the latter also when the text has no strong letters).
        /// </returns>
        public static TextDirection GetBaseDirection(string? text, bool forceRtlWhenPersian)
        {
            if (text == null || text.Length == 0)
            {
                return TextDirection.LeftToRight;
            }

            foreach (char c in text)
            {
                if (IsRtlLetter(c))
                {
                    return TextDirection.RightToLeft;
                }

                if (!forceRtlWhenPersian && char.IsLetter(c))
                {
                    return TextDirection.LeftToRight;
                }
            }

            return TextDirection.LeftToRight;
        }
    }
}
