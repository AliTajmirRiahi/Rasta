using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasta.Text
{
    /// <summary>
    /// Splits cleaned comment text into runs so that embedded opposite-direction fragments
    /// (English words, identifiers, numbers, paths, URLs inside Persian text) can be isolated.
    /// Pure logic with no Visual Studio dependencies.
    /// </summary>
    public static class BidiRunSplitter
    {
        private const string RtlBlocks = @"؀-ۿݐ-ݿࢠ-ࣿﭐ-﷿ﹰ-﻿";
        private const string ArabicPunctuation = @"،؛؟٪-٭۔";

        // A left-to-right word character: any word character that is not Arabic-script.
        private const string LtrWord = @"[\w-[" + RtlBlocks + "]]";

        // Punctuation that may sit between two words of the same run (URLs, paths, phrases).
        private const string Joiners = @"[.,;:!?\-/\\@#&=+%~$'""*|]";

        private const string LtrBracketGroup =
            @"(?:\([^()\r\n" + RtlBlocks + @"]*\)|\[[^\[\]\r\n" + RtlBlocks + @"]*\]|<[^<>\r\n" + RtlBlocks + @"]*>|\{[^{}\r\n" + RtlBlocks + @"]*\})";

        private const string LtrGap = @"(?:[ \t]*" + Joiners + @"+[ \t]*|[ \t]+)";

        private static readonly Regex LtrRun = new Regex(
            @"(?:[@#$~./\\]+(?=" + LtrWord + "))?" + LtrWord + "+" +
            "(?:" + LtrGap + "?" + "(?:" + LtrWord + "+|" + LtrBracketGroup + "))*",
            RegexOptions.CultureInvariant);

        // A right-to-left word character: Arabic-script letters/digits/marks plus ZWNJ and ZWJ.
        private const string RtlWord = @"[" + RtlBlocks + @"‌‍-[" + ArabicPunctuation + "]]";

        private const string RtlJoiners = @"[.,;:!?\-/\\'""*|" + ArabicPunctuation + "]";

        private const string RtlGap = @"(?:[ \t]*" + RtlJoiners + @"+[ \t]*|[ \t]+)";

        private static readonly Regex RtlRun = new Regex(
            RtlWord + "+" + "(?:" + RtlGap + "?" + RtlWord + "+)*",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Splits <paramref name="text"/> into runs.
        /// </summary>
        /// <param name="text">Cleaned comment text; lines are separated by <c>\n</c>.</param>
        /// <param name="baseDirection">
        /// The base direction of the paragraph. Runs of the opposite direction are reported as
        /// <see cref="BidiRunKind.Opposite"/>.
        /// </param>
        /// <returns>The ordered runs; concatenating their text (and <c>\n</c> for line breaks, backticks for code) restores the input.</returns>
        public static IReadOnlyList<BidiRun> Split(string? text, TextDirection baseDirection)
        {
            var runs = new List<BidiRun>();
            if (text == null || text.Length == 0)
            {
                return runs;
            }

            Regex oppositePattern = baseDirection == TextDirection.RightToLeft ? LtrRun : RtlRun;
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    runs.Add(new BidiRun(string.Empty, BidiRunKind.LineBreak));
                }

                SplitLine(lines[i], oppositePattern, runs);
            }

            return runs;
        }

        private static void SplitLine(string line, Regex oppositePattern, List<BidiRun> runs)
        {
            int position = 0;
            while (position < line.Length)
            {
                int open = line.IndexOf('`', position);
                if (open < 0)
                {
                    break;
                }

                int close = line.IndexOf('`', open + 1);
                if (close < 0)
                {
                    break;
                }

                if (close == open + 1)
                {
                    // An empty pair of backticks is not code; keep both characters as text.
                    SplitPlain(line.Substring(position, close + 1 - position), oppositePattern, runs);
                    position = close + 1;
                    continue;
                }

                SplitPlain(line.Substring(position, open - position), oppositePattern, runs);
                runs.Add(new BidiRun(line.Substring(open + 1, close - open - 1), BidiRunKind.Code));
                position = close + 1;
            }

            if (position < line.Length)
            {
                SplitPlain(line.Substring(position), oppositePattern, runs);
            }
        }

        private static void SplitPlain(string segment, Regex oppositePattern, List<BidiRun> runs)
        {
            if (segment.Length == 0)
            {
                return;
            }

            int last = 0;
            for (Match m = oppositePattern.Match(segment); m.Success; m = m.NextMatch())
            {
                if (m.Index > last)
                {
                    runs.Add(new BidiRun(segment.Substring(last, m.Index - last), BidiRunKind.Text));
                }

                runs.Add(new BidiRun(m.Value, BidiRunKind.Opposite));
                last = m.Index + m.Length;
            }

            if (last < segment.Length)
            {
                runs.Add(new BidiRun(segment.Substring(last), BidiRunKind.Text));
            }
        }
    }
}
