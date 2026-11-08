using System;

namespace Rasta.Options
{
    /// <summary>
    /// An immutable, validated snapshot of the Rasta options. The editor components read
    /// <see cref="Current"/> so that they never touch Visual Studio's settings store on a hot path.
    /// </summary>
    public sealed class RastaSettings
    {
        /// <summary>The default font stack used by the popup.</summary>
        public const string DefaultFontFamily = "Vazirmatn, Segoe UI, Tahoma";

        private static volatile RastaSettings current = new RastaSettings(
            enabled: true,
            fontFamily: DefaultFontFamily,
            fontSize: 14,
            maxWidth: 600,
            minimumRtlCharacters: 1,
            forceRtl: true,
            mergeConsecutiveLines: true,
            showHeader: true);

        /// <summary>Initializes a new instance of the <see cref="RastaSettings"/> class, clamping out-of-range values.</summary>
        /// <param name="enabled">Whether the extension is active.</param>
        /// <param name="fontFamily">The WPF font family stack.</param>
        /// <param name="fontSize">The font size in device-independent pixels.</param>
        /// <param name="maxWidth">The maximum popup width in device-independent pixels.</param>
        /// <param name="minimumRtlCharacters">The minimum number of RTL letters required to show the popup.</param>
        /// <param name="forceRtl">Whether any Persian text forces a right-to-left base direction.</param>
        /// <param name="mergeConsecutiveLines">Whether adjacent comment lines are shown as one comment.</param>
        /// <param name="showHeader">Whether the Rasta header is shown in the popup.</param>
        public RastaSettings(
            bool enabled,
            string? fontFamily,
            int fontSize,
            int maxWidth,
            int minimumRtlCharacters,
            bool forceRtl,
            bool mergeConsecutiveLines,
            bool showHeader)
        {
            Enabled = enabled;
            FontFamily = string.IsNullOrWhiteSpace(fontFamily) ? DefaultFontFamily : fontFamily!.Trim();
            FontSize = Math.Max(6, Math.Min(72, fontSize));
            MaxWidth = Math.Max(200, Math.Min(2000, maxWidth));
            MinimumRtlCharacters = Math.Max(1, Math.Min(1000, minimumRtlCharacters));
            ForceRtl = forceRtl;
            MergeConsecutiveLines = mergeConsecutiveLines;
            ShowHeader = showHeader;
        }

        /// <summary>Gets the settings currently in effect; defaults until the package has loaded the user's options.</summary>
        public static RastaSettings Current => current;

        /// <summary>Gets a value indicating whether the extension is active.</summary>
        public bool Enabled { get; }

        /// <summary>Gets the WPF font family stack.</summary>
        public string FontFamily { get; }

        /// <summary>Gets the font size in device-independent pixels.</summary>
        public int FontSize { get; }

        /// <summary>Gets the maximum popup width in device-independent pixels.</summary>
        public int MaxWidth { get; }

        /// <summary>Gets the minimum number of RTL letters required to show the popup.</summary>
        public int MinimumRtlCharacters { get; }

        /// <summary>Gets a value indicating whether any Persian text forces a right-to-left base direction.</summary>
        public bool ForceRtl { get; }

        /// <summary>Gets a value indicating whether adjacent comment lines are shown as one comment.</summary>
        public bool MergeConsecutiveLines { get; }

        /// <summary>Gets a value indicating whether the Rasta header is shown in the popup.</summary>
        public bool ShowHeader { get; }

        /// <summary>Replaces <see cref="Current"/>; the change is visible to the next hover immediately.</summary>
        /// <param name="settings">The new settings.</param>
        public static void Publish(RastaSettings settings)
        {
            current = settings;
        }
    }
}
