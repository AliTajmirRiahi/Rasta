using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Rasta.QuickInfo
{
    /// <summary>
    /// Creates a <see cref="RastaQuickInfoSource"/> for every text buffer. The source itself decides,
    /// per hover, whether it has anything to show, so the provider is cheap and always returns one.
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Rasta QuickInfo")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType("code")]
    [ContentType("text")]
    internal sealed class RastaQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        private readonly IViewClassifierAggregatorService classifierAggregatorService;

        /// <summary>Initializes a new instance of the <see cref="RastaQuickInfoSourceProvider"/> class.</summary>
        /// <param name="classifierAggregatorService">
        /// The service that provides a view's combined classifier. The view-level aggregator is required because
        /// language services such as Roslyn contribute their classifications per view, not per buffer.
        /// </param>
        [ImportingConstructor]
        public RastaQuickInfoSourceProvider(IViewClassifierAggregatorService classifierAggregatorService)
        {
            this.classifierAggregatorService = classifierAggregatorService;
        }

        /// <inheritdoc />
        public IAsyncQuickInfoSource? TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new RastaQuickInfoSource(textBuffer, classifierAggregatorService);
        }
    }
}
