using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Rasta.QuickInfo
{
    /// <summary>
    /// Creates the per-view classifier state when an editor opens, so the first hover does not pay for it.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class RastaViewWarmup : IWpfTextViewCreationListener
    {
        private readonly IViewClassifierAggregatorService classifierAggregatorService;

        /// <summary>Initializes a new instance of the <see cref="RastaViewWarmup"/> class.</summary>
        /// <param name="classifierAggregatorService">The service that provides a view's combined classifier.</param>
        [ImportingConstructor]
        public RastaViewWarmup(IViewClassifierAggregatorService classifierAggregatorService)
        {
            this.classifierAggregatorService = classifierAggregatorService;
        }

        /// <inheritdoc />
        public void TextViewCreated(IWpfTextView textView)
        {
            try
            {
                // Called on the UI thread, which is where classifiers must be created.
                RastaBufferState.GetOrCreate(textView, classifierAggregatorService);
            }
            catch (System.Exception ex)
            {
                Logging.RastaLog.Error("Warming up the text view", ex);
            }
        }
    }
}
