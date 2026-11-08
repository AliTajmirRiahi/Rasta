using Microsoft.VisualStudio.Text;

namespace Rasta.Comments
{
    /// <summary>A logical comment found in a text buffer, possibly merged from several adjacent lines.</summary>
    internal sealed class CommentInfo
    {
        /// <summary>Initializes a new instance of the <see cref="CommentInfo"/> class.</summary>
        /// <param name="span">The span of the comment in the snapshot it was extracted from.</param>
        /// <param name="rawText">The comment text exactly as it appears in the source.</param>
        public CommentInfo(Span span, string rawText)
        {
            Span = span;
            RawText = rawText;
        }

        /// <summary>Gets the span of the comment in the snapshot it was extracted from.</summary>
        public Span Span { get; }

        /// <summary>Gets the comment text exactly as it appears in the source, markers included.</summary>
        public string RawText { get; }
    }
}
