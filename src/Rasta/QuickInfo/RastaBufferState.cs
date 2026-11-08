using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Rasta.QuickInfo
{
    /// <summary>A comment that has already been extracted and cleaned for one snapshot version.</summary>
    internal sealed class CachedComment
    {
        /// <summary>Initializes a new instance of the <see cref="CachedComment"/> class.</summary>
        /// <param name="versionNumber">The snapshot version the comment was extracted from.</param>
        /// <param name="merged">Whether adjacent comment lines were merged.</param>
        /// <param name="span">The comment span in that snapshot.</param>
        /// <param name="text">The cleaned display text.</param>
        public CachedComment(int versionNumber, bool merged, Span span, string text)
        {
            VersionNumber = versionNumber;
            Merged = merged;
            Span = span;
            Text = text;
        }

        /// <summary>Gets the snapshot version the comment was extracted from.</summary>
        public int VersionNumber { get; }

        /// <summary>Gets a value indicating whether adjacent comment lines were merged.</summary>
        public bool Merged { get; }

        /// <summary>Gets the comment span in the snapshot it was extracted from.</summary>
        public Span Span { get; }

        /// <summary>Gets the cleaned display text.</summary>
        public string Text { get; }
    }

    /// <summary>
    /// Per-view state shared by every QuickInfo session: the view's classifier and the most
    /// recently extracted comment. Stored on the text view's property bag so it lives exactly as long as the view.
    /// </summary>
    internal sealed class RastaBufferState
    {
        private readonly object gate = new object();
        private CachedComment? last;

        /// <summary>Initializes a new instance of the <see cref="RastaBufferState"/> class.</summary>
        /// <param name="classifier">The aggregated classifier of the text view.</param>
        public RastaBufferState(IClassifier classifier)
        {
            Classifier = classifier;
        }

        /// <summary>Gets the aggregated classifier of the text view.</summary>
        public IClassifier Classifier { get; }

        /// <summary>Returns the state of <paramref name="textView"/>, creating it on first use. UI thread only.</summary>
        /// <param name="textView">The view.</param>
        /// <param name="service">The service that provides the view's classifier.</param>
        /// <returns>The shared per-view state.</returns>
        public static RastaBufferState GetOrCreate(Microsoft.VisualStudio.Text.Editor.ITextView textView, IViewClassifierAggregatorService service)
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                typeof(RastaBufferState), () => new RastaBufferState(service.GetClassifier(textView)));
        }

        /// <summary>Returns the cached comment when it still covers <paramref name="position"/> in the same version.</summary>
        /// <param name="versionNumber">The current snapshot version.</param>
        /// <param name="position">The hover position.</param>
        /// <param name="merged">Whether adjacent comment lines are being merged.</param>
        /// <returns>The cached comment, or <c>null</c> on a miss.</returns>
        public CachedComment? TryGet(int versionNumber, int position, bool merged)
        {
            lock (gate)
            {
                CachedComment? cached = last;
                return cached != null
                    && cached.VersionNumber == versionNumber
                    && cached.Merged == merged
                    && cached.Span.Contains(position)
                    ? cached
                    : null;
            }
        }

        /// <summary>Remembers <paramref name="comment"/> as the most recent result.</summary>
        /// <param name="comment">The comment to cache.</param>
        public void Store(CachedComment comment)
        {
            lock (gate)
            {
                last = comment;
            }
        }
    }
}
