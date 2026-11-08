using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Rasta.Comments
{
    /// <summary>
    /// Locates the comment under a text position by asking the editor's classifier, so that the
    /// language service decides what is a comment rather than a text search.
    /// </summary>
    internal static class CommentExtractor
    {
        private const int MaxMergedLinesPerDirection = 200;
        private const string BlockKey = "block";

        /// <summary>
        /// Returns <c>true</c> when a classification type denotes comment text. Besides types that
        /// derive from the predefined <c>comment</c> type, this accepts the comment families that
        /// language services register separately (XML doc, XML, HTML, Razor, ...), identified by
        /// their classification names.
        /// </summary>
        /// <param name="type">The classification type to test.</param>
        public static bool IsCommentClassification(IClassificationType type)
        {
            return type.IsOfType(PredefinedClassificationTypeNames.Comment)
                || type.Classification.IndexOf("comment", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Finds the comment that contains <paramref name="position"/>.</summary>
        /// <param name="snapshot">The snapshot to inspect.</param>
        /// <param name="position">The position under the mouse.</param>
        /// <param name="classifier">The buffer's classifier.</param>
        /// <param name="mergeLines">Whether adjacent full-line comments are merged into one comment.</param>
        /// <param name="cancellationToken">A token that aborts the search.</param>
        /// <returns>The comment, or <c>null</c> when the position is not inside a comment.</returns>
        public static CommentInfo? Extract(
            ITextSnapshot snapshot,
            int position,
            IClassifier classifier,
            bool mergeLines,
            CancellationToken cancellationToken)
        {
            if (position < 0 || position > snapshot.Length)
            {
                return null;
            }

            ITextSnapshotLine hoveredLine = snapshot.GetLineFromPosition(position);
            LineGroup? hovered = FindGroup(snapshot, hoveredLine, position, classifier);
            if (hovered == null)
            {
                return null;
            }

            int start = hovered.Value.Start;
            int end = hovered.Value.End;

            if (mergeLines && IsFullLine(snapshot, hoveredLine, hovered.Value))
            {
                string key = hovered.Value.Key;
                int lineNumber = hoveredLine.LineNumber;

                for (int n = lineNumber - 1, steps = 0; n >= 0 && steps < MaxMergedLinesPerDirection; n--, steps++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    LineGroup? above = FindNeighbour(snapshot, snapshot.GetLineFromLineNumber(n), key, classifier);
                    if (above == null)
                    {
                        break;
                    }

                    start = above.Value.Start;
                }

                for (int n = lineNumber + 1, steps = 0; n < snapshot.LineCount && steps < MaxMergedLinesPerDirection; n++, steps++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    LineGroup? below = FindNeighbour(snapshot, snapshot.GetLineFromLineNumber(n), key, classifier);
                    if (below == null)
                    {
                        break;
                    }

                    end = below.Value.End;
                }
            }

            var span = new Span(start, end - start);
            return new CommentInfo(span, snapshot.GetText(span));
        }

        private static LineGroup? FindNeighbour(ITextSnapshot snapshot, ITextSnapshotLine line, string key, IClassifier classifier)
        {
            int first = FirstNonWhitespace(snapshot, line);
            if (first < 0)
            {
                return null;
            }

            LineGroup? group = FindGroup(snapshot, line, first, classifier);
            return group != null && group.Value.Key == key && IsFullLine(snapshot, line, group.Value) ? group : null;
        }

        private static bool IsFullLine(ITextSnapshot snapshot, ITextSnapshotLine line, LineGroup group)
        {
            int first = FirstNonWhitespace(snapshot, line);
            int last = LastNonWhitespace(snapshot, line);
            return first >= 0 && group.Start <= first && group.End > last;
        }

        /// <summary>
        /// Computes the comment region on <paramref name="line"/> that contains <paramref name="position"/>.
        /// </summary>
        private static LineGroup? FindGroup(ITextSnapshot snapshot, ITextSnapshotLine line, int position, IClassifier classifier)
        {
            IList<ClassificationSpan> spans = classifier.GetClassificationSpans(line.Extent);
            var intervals = new List<Interval>(spans.Count);
            int hitIndex = -1;
            foreach (ClassificationSpan span in spans)
            {
                if (!IsCommentClassification(span.ClassificationType))
                {
                    continue;
                }

                int s = Math.Max(span.Span.Start.Position, line.Start.Position);
                int e = Math.Min(span.Span.End.Position, line.End.Position);
                if (e > s)
                {
                    intervals.Add(new Interval(s, e));
                }
            }

            if (intervals.Count == 0)
            {
                return null;
            }

            intervals.Sort((a, b) => a.Start.CompareTo(b.Start));
            for (int i = 0; i < intervals.Count; i++)
            {
                if (position >= intervals[i].Start && position < intervals[i].End)
                {
                    hitIndex = i;
                    break;
                }
            }

            if (hitIndex < 0)
            {
                return null;
            }

            Interval hit = intervals[hitIndex];

            // A line comment (//, ///, #, --, ') runs to the end of the line even when the language
            // service classifies parts of it differently (for example a cref inside an XML doc comment).
            for (int i = hitIndex; i >= 0; i--)
            {
                string? lineKey = GetLineCommentKey(snapshot, intervals[i].Start, intervals[i].End);
                if (lineKey != null)
                {
                    int lastNonWhitespace = LastNonWhitespace(snapshot, line);
                    return new LineGroup(intervals[i].Start, Math.Max(lastNonWhitespace + 1, hit.End), lineKey);
                }
            }

            // Otherwise grow the hit over neighbouring comment spans separated only by whitespace.
            int groupStart = hit.Start;
            int groupEnd = hit.End;
            for (int i = hitIndex - 1; i >= 0 && IsWhitespaceBetween(snapshot, intervals[i].End, groupStart); i--)
            {
                groupStart = Math.Min(groupStart, intervals[i].Start);
            }

            for (int i = hitIndex + 1; i < intervals.Count && IsWhitespaceBetween(snapshot, groupEnd, intervals[i].Start); i++)
            {
                groupEnd = Math.Max(groupEnd, intervals[i].End);
            }

            return new LineGroup(groupStart, groupEnd, BlockKey);
        }

        private static string? GetLineCommentKey(ITextSnapshot snapshot, int start, int end)
        {
            int length = Math.Min(3, end - start);
            string head = snapshot.GetText(start, length);
            if (head.StartsWith("///", StringComparison.Ordinal))
            {
                return "///";
            }

            if (head.StartsWith("//", StringComparison.Ordinal))
            {
                return "//";
            }

            if (head.StartsWith("'''", StringComparison.Ordinal))
            {
                return "'''";
            }

            if (head.StartsWith("--", StringComparison.Ordinal) && !head.StartsWith("-->", StringComparison.Ordinal))
            {
                return "--";
            }

            return head.Length > 0 && (head[0] == '#' || head[0] == '\'') ? head.Substring(0, 1) : null;
        }

        private static bool IsWhitespaceBetween(ITextSnapshot snapshot, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                if (!char.IsWhiteSpace(snapshot[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static int FirstNonWhitespace(ITextSnapshot snapshot, ITextSnapshotLine line)
        {
            for (int i = line.Start.Position; i < line.End.Position; i++)
            {
                if (!char.IsWhiteSpace(snapshot[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int LastNonWhitespace(ITextSnapshot snapshot, ITextSnapshotLine line)
        {
            for (int i = line.End.Position - 1; i >= line.Start.Position; i--)
            {
                if (!char.IsWhiteSpace(snapshot[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly struct Interval
        {
            public Interval(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }

            public int End { get; }
        }

        private readonly struct LineGroup
        {
            public LineGroup(int start, int end, string key)
            {
                Start = start;
                End = end;
                Key = key;
            }

            public int Start { get; }

            public int End { get; }

            public string Key { get; }
        }
    }
}
