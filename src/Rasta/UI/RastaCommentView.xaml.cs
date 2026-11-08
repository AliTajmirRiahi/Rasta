using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.PlatformUI;
using Rasta.Logging;
using Rasta.Options;
using Rasta.Text;

namespace Rasta.UI
{
    /// <summary>
    /// The popup content: the cleaned comment rendered right-to-left with its embedded left-to-right
    /// fragments isolated, a small Rasta header and a Copy button. All colors come from the Visual Studio theme.
    /// </summary>
    public partial class RastaCommentView : UserControl
    {
        // WPF's bidi engine predates the isolate controls added in Unicode 6.3, so the classic embeddings are used.
        private const string LeftToRightEmbedding = "‪";
        private const string RightToLeftEmbedding = "‫";
        private const string PopDirectionalFormatting = "‬";
        private const string CodeFontFamily = "Cascadia Mono, Consolas, Courier New";
        private const double WidthSlack = 28;

        private readonly string commentText;

        /// <summary>Initializes a new instance of the <see cref="RastaCommentView"/> class.</summary>
        /// <param name="commentText">The cleaned comment text; lines are separated by <c>\n</c>.</param>
        /// <param name="baseDirection">The base direction of the paragraph.</param>
        /// <param name="settings">The font, width and header settings.</param>
        public RastaCommentView(string commentText, TextDirection baseDirection, RastaSettings settings)
        {
            InitializeComponent();
            this.commentText = commentText;

            try
            {
                HeaderIcon.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Rasta;component/Resources/Icon.png", UriKind.Absolute));
                HeaderIcon.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                // The icon is decorative; the popup works without it.
            }

            MaxWidth = settings.MaxWidth;
            HeaderGrid.Visibility = settings.ShowHeader ? Visibility.Visible : Visibility.Collapsed;

            FlowDirection flow = baseDirection == TextDirection.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            CommentBox.Document = BuildDocument(commentText, baseDirection, flow, settings);
            CommentBox.Width = MeasureWidth(commentText, flow, settings);
        }

        private static FlowDocument BuildDocument(string text, TextDirection baseDirection, FlowDirection flow, RastaSettings settings)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0) };
            string opposite = baseDirection == TextDirection.RightToLeft ? LeftToRightEmbedding : RightToLeftEmbedding;
            var codeFont = new FontFamily(CodeFontFamily);

            foreach (BidiRun run in BidiRunSplitter.Split(text, baseDirection))
            {
                switch (run.Kind)
                {
                    case BidiRunKind.LineBreak:
                        paragraph.Inlines.Add(new LineBreak());
                        break;
                    case BidiRunKind.Opposite:
                        paragraph.Inlines.Add(new Run(opposite + run.Text + PopDirectionalFormatting));
                        break;
                    case BidiRunKind.Code:
                        paragraph.Inlines.Add(new Run(LeftToRightEmbedding + run.Text + PopDirectionalFormatting) { FontFamily = codeFont });
                        break;
                    default:
                        paragraph.Inlines.Add(new Run(run.Text));
                        break;
                }
            }

            // TextAlignment.Left is flow-relative in WPF: it aligns to the leading edge, which is the right edge for RTL.
            var document = new FlowDocument(paragraph)
            {
                FlowDirection = flow,
                TextAlignment = TextAlignment.Left,
                FontFamily = new FontFamily(settings.FontFamily),
                FontSize = settings.FontSize,
                PagePadding = new Thickness(0),
            };
            document.SetResourceReference(TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
            return document;
        }

        private double MeasureWidth(string text, FlowDirection flow, RastaSettings settings)
        {
            var typeface = new Typeface(new FontFamily(settings.FontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double widest = 0;
            foreach (string line in text.Replace("`", string.Empty).Split('\n'))
            {
                var formatted = new FormattedText(line, CultureInfo.CurrentUICulture, flow, typeface, settings.FontSize, Brushes.Black, pixelsPerDip);
                widest = Math.Max(widest, formatted.WidthIncludingTrailingWhitespace);
            }

            return Math.Min(settings.MaxWidth, Math.Ceiling(widest) + WidthSlack);
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetDataObject(commentText, true);
                CopyButton.Content = "Copied";
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    CopyButton.Content = "Copy";
                };
                timer.Start();
            }
            catch (ExternalException ex)
            {
                RastaLog.Error("Copying the comment to the clipboard", ex);
            }
        }
    }
}
