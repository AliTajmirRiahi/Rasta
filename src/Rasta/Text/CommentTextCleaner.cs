using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Rasta.Text
{
    /// <summary>
    /// Turns raw comment source text into clean display text: strips comment markers and
    /// XML documentation tags and normalises whitespace. Zero-width non-joiners are preserved.
    /// Pure logic with no Visual Studio dependencies.
    /// </summary>
    public static class CommentTextCleaner
    {
        private const RegexOptions Options = RegexOptions.CultureInvariant;

        private static readonly Regex LeadingMarker = new Regex(
            @"^[ \t]*(?:/{2,}|/\*+|@\*+|\*(?!/)|'+|<!--+|#+|-{2,})[ \t]?",
            Options);

        private static readonly Regex TrailingMarker = new Regex(
            @"[ \t]*(?:\*+/|\*+@|--+>)[ \t]*$",
            Options);

        private static readonly Regex KnownTagProbe = new Regex(
            @"</?(?:summary|remarks|returns|value|example|para|list|listheader|item|term|description|br|param|typeparam|paramref|typeparamref|see|seealso|c|code|exception|inheritdoc|include|permission|completionlist|b|i|u|em|strong)\b[^<>]*>",
            Options | RegexOptions.IgnoreCase);

        private static readonly Regex ParamRef = new Regex(
            @"<(?:param|typeparam)ref\s+name\s*=\s*[""']([^""']*)[""']\s*/?>(?:\s*</(?:param|typeparam)ref>)?",
            Options | RegexOptions.IgnoreCase);

        private static readonly Regex SeeTag = new Regex(
            @"<see(?:also)?\s+[^<>]*?(?:cref|langword|href)\s*=\s*[""']([^""']*)[""'][^<>]*?(?:/>|>(.*?)</see(?:also)?>)",
            Options | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex InlineCode = new Regex(
            @"<(c|code)>(.*?)</\1>",
            Options | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex LabeledTag = new Regex(
            @"<(param|typeparam|exception)\s+(?:name|cref)\s*=\s*[""']([^""']*)[""']\s*(?:/>|>(.*?)</\1>)",
            Options | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex BlockTag = new Regex(
            @"</?(?:summary|remarks|returns|value|example|para|list|listheader|item|term|description|br)\b[^<>]*>",
            Options | RegexOptions.IgnoreCase);

        private static readonly Regex DiscardedTag = new Regex(
            @"</?(?:inheritdoc|include|permission|completionlist|b|i|u|em|strong)\b[^<>]*>",
            Options | RegexOptions.IgnoreCase);

        private static readonly Regex Spaces = new Regex(@"[ \t ]+", Options);

        private static readonly Regex CrefPrefix = new Regex(@"^[A-Za-z]:", Options);

        /// <summary>
        /// Cleans raw comment text.
        /// </summary>
        /// <param name="rawComment">The comment exactly as it appears in the source, markers included.</param>
        /// <returns>
        /// The display text with <c>\n</c> line separators, or an empty string when the comment has no content.
        /// </returns>
        public static string Clean(string? rawComment)
        {
            if (rawComment == null || rawComment.Trim().Length == 0)
            {
                return string.Empty;
            }

            string[] rawLines = rawComment.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var stripped = new StringBuilder(rawComment.Length);
            for (int i = 0; i < rawLines.Length; i++)
            {
                if (i > 0)
                {
                    stripped.Append('\n');
                }

                stripped.Append(StripMarkers(rawLines[i]));
            }

            string text = stripped.ToString();
            if (text.IndexOf('<') >= 0 && KnownTagProbe.IsMatch(text))
            {
                text = ConvertXmlDoc(text);
            }

            return NormalizeLines(text);
        }

        private static string StripMarkers(string line)
        {
            string result = LeadingMarker.Replace(line, string.Empty, 1);
            return TrailingMarker.Replace(result, string.Empty, 1);
        }

        private static string ConvertXmlDoc(string text)
        {
            text = ParamRef.Replace(text, "$1");
            text = SeeTag.Replace(text, EvaluateSee);
            text = InlineCode.Replace(text, EvaluateInlineCode);
            text = LabeledTag.Replace(text, EvaluateLabeled);
            text = BlockTag.Replace(text, "\n");
            text = DiscardedTag.Replace(text, string.Empty);
            return WebUtility.HtmlDecode(text);
        }

        private static string EvaluateSee(Match m)
        {
            string inner = m.Groups[2].Success ? m.Groups[2].Value.Trim() : string.Empty;
            if (inner.Length > 0)
            {
                return inner;
            }

            string target = CrefPrefix.Replace(m.Groups[1].Value, string.Empty, 1);
            return target.Replace('{', '<').Replace('}', '>');
        }

        private static string EvaluateInlineCode(Match m)
        {
            string content = m.Groups[2].Value.Trim('\r', '\n');
            if (content.Trim().Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (string codeLine in content.Replace("\r", string.Empty).Split('\n'))
            {
                string trimmed = codeLine.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append('\n');
                }

                sb.Append('`').Append(trimmed.Replace("`", string.Empty)).Append('`');
            }

            return sb.ToString();
        }

        private static string EvaluateLabeled(Match m)
        {
            string label = CrefPrefix.Replace(m.Groups[2].Value, string.Empty, 1).Replace('{', '<').Replace('}', '>');
            string body = m.Groups[3].Success ? m.Groups[3].Value.Trim() : string.Empty;
            return body.Length == 0
                ? "\n" + label + "\n"
                : "\n" + label + " — " + body + "\n";
        }

        private static string NormalizeLines(string text)
        {
            var lines = new List<string>();
            bool previousBlank = true;
            foreach (string line in text.Replace("\r", string.Empty).Split('\n'))
            {
                string normalized = Spaces.Replace(line, " ").Trim();
                if (normalized.Length == 0)
                {
                    if (!previousBlank)
                    {
                        lines.Add(string.Empty);
                    }

                    previousBlank = true;
                    continue;
                }

                lines.Add(normalized);
                previousBlank = false;
            }

            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines);
        }
    }
}
