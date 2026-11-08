using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Rasta.Comments;
using Rasta.Logging;
using Rasta.Options;
using Rasta.Text;
using Rasta.UI;

namespace Rasta.QuickInfo
{
    /// <summary>
    /// Supplies the Rasta popup for comments that contain Persian/Arabic text. It never throws into the
    /// editor: any failure is logged and the hover simply shows nothing from Rasta.
    /// </summary>
    internal sealed class RastaQuickInfoSource : IAsyncQuickInfoSource
    {
        private static readonly object SessionClaimKey = new object();
        private readonly ITextBuffer textBuffer;
        private readonly IViewClassifierAggregatorService classifierAggregatorService;

        /// <summary>Initializes a new instance of the <see cref="RastaQuickInfoSource"/> class.</summary>
        /// <param name="textBuffer">The buffer this source was created for.</param>
        /// <param name="classifierAggregatorService">The service that provides a view's combined classifier.</param>
        public RastaQuickInfoSource(ITextBuffer textBuffer, IViewClassifierAggregatorService classifierAggregatorService)
        {
            this.textBuffer = textBuffer;
            this.classifierAggregatorService = classifierAggregatorService;
        }

        /// <inheritdoc />
        public async Task<QuickInfoItem?> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            try
            {
                RastaSettings settings = RastaSettings.Current;
                ITextView? textView = session.TextView;
                if (!settings.Enabled || textView == null || textView.TextBuffer != textBuffer)
                {
                    // Only the source of the view's own buffer answers, so projection buffers do not produce duplicates.
                    return null;
                }

                lock (session.Properties)
                {
                    // The broker may ask more than one Rasta source per hover; only the first answers.
                    if (session.Properties.ContainsProperty(SessionClaimKey))
                    {
                        return null;
                    }

                    session.Properties.AddProperty(SessionClaimKey, this);
                }

                ITextSnapshot snapshot = textView.TextSnapshot;
                SnapshotPoint? trigger = session.GetTriggerPoint(snapshot);
                if (trigger == null)
                {
                    return null;
                }

                int position = trigger.Value.Position;
                int version = snapshot.Version.VersionNumber;
                textView.Properties.TryGetProperty(typeof(RastaBufferState), out RastaBufferState? state);
                CachedComment? comment = state?.TryGet(version, position, settings.MergeConsecutiveLines);
                if (comment == null)
                {
                    // Language-service classifiers (Roslyn's among them) must be created and queried on the UI thread.
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    state = RastaBufferState.GetOrCreate(textView, classifierAggregatorService);
                    CommentInfo? info = CommentExtractor.Extract(
                        snapshot, position, state.Classifier, settings.MergeConsecutiveLines, cancellationToken);
                    if (info == null)
                    {
                        return null;
                    }

                    // Cleaning takes microseconds; staying on the UI thread avoids two thread hops.
                    comment = new CachedComment(version, settings.MergeConsecutiveLines, info.Span, CommentTextCleaner.Clean(info.RawText));
                    state.Store(comment);
                }

                if (!RtlTextDetector.ContainsRtl(comment.Text, settings.MinimumRtlCharacters))
                {
                    return null;
                }

                TextDirection direction = RtlTextDetector.GetBaseDirection(comment.Text, settings.ForceRtl);
                ITrackingSpan applicableTo = snapshot.CreateTrackingSpan(comment.Span, SpanTrackingMode.EdgeInclusive);

                // No-op when already on the UI thread (cache miss path above).
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var view = new RastaCommentView(comment.Text, direction, settings);
                return new QuickInfoItem(applicableTo, view);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                RastaLog.Error("Computing the QuickInfo popup", ex);
                return null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // The source holds no unmanaged resources; shared state lives on the text view.
        }
    }
}
