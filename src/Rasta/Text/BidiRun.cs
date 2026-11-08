namespace Rasta.Text
{
    /// <summary>The role a <see cref="BidiRun"/> plays when the comment is rendered.</summary>
    public enum BidiRunKind
    {
        /// <summary>Text that follows the base direction of the paragraph.</summary>
        Text,

        /// <summary>
        /// A run whose direction is opposite to the base direction (for example an English identifier
        /// inside Persian text) and which must be isolated so that its punctuation does not flip.
        /// </summary>
        Opposite,

        /// <summary>Inline code, always rendered left-to-right in a monospace font.</summary>
        Code,

        /// <summary>A line break between two lines of the comment.</summary>
        LineBreak,
    }

    /// <summary>A contiguous piece of text with a uniform rendering role.</summary>
    public sealed class BidiRun
    {
        /// <summary>Initializes a new instance of the <see cref="BidiRun"/> class.</summary>
        /// <param name="text">The run text; empty for line breaks.</param>
        /// <param name="kind">The rendering role of the run.</param>
        public BidiRun(string text, BidiRunKind kind)
        {
            Text = text;
            Kind = kind;
        }

        /// <summary>Gets the run text.</summary>
        public string Text { get; }

        /// <summary>Gets the rendering role of the run.</summary>
        public BidiRunKind Kind { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Kind + ":" + Text;
        }
    }
}
