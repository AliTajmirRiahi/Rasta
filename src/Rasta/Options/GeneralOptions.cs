using System.ComponentModel;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;

namespace Rasta.Options
{
    /// <summary>Hosts the Tools &gt; Options &gt; Rasta option pages.</summary>
    internal partial class OptionsProvider
    {
        /// <summary>The Tools &gt; Options &gt; Rasta &gt; General page.</summary>
        [ComVisible(true)]
        public class GeneralOptionsPage : BaseOptionPage<GeneralOptions>
        {
        }
    }

    /// <summary>The persisted Rasta options edited on the General options page.</summary>
    public class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        /// <summary>Gets or sets a value indicating whether Rasta shows its hover popup.</summary>
        [Category("Behavior")]
        [DisplayName("Enabled")]
        [Description("Show the Rasta hover popup for comments that contain Persian or Arabic text.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>Gets or sets the minimum number of RTL letters a comment needs before the popup appears.</summary>
        [Category("Behavior")]
        [DisplayName("Minimum RTL characters")]
        [Description("Minimum number of Persian/Arabic letters a comment must contain to trigger the popup.")]
        [DefaultValue(1)]
        public int MinimumRtlCharacters { get; set; } = 1;

        /// <summary>Gets or sets a value indicating whether Persian text forces a right-to-left base direction.</summary>
        [Category("Behavior")]
        [DisplayName("Force RTL when Persian is present")]
        [Description("When enabled, any comment containing Persian is laid out right-to-left. When disabled, the first strong letter decides the direction.")]
        [DefaultValue(true)]
        public bool ForceRtl { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether adjacent comment lines are merged.</summary>
        [Category("Behavior")]
        [DisplayName("Merge consecutive comment lines")]
        [Description("Show consecutive // or /// lines, or a whole /* */ block, as one paragraph.")]
        [DefaultValue(true)]
        public bool MergeConsecutiveLines { get; set; } = true;

        /// <summary>Gets or sets the font family stack used by the popup.</summary>
        [Category("Appearance")]
        [DisplayName("Font family")]
        [Description("Comma-separated list of font families, tried in order.")]
        [DefaultValue(RastaSettings.DefaultFontFamily)]
        public string FontFamily { get; set; } = RastaSettings.DefaultFontFamily;

        /// <summary>Gets or sets the popup font size.</summary>
        [Category("Appearance")]
        [DisplayName("Font size")]
        [Description("Font size of the popup text, in device-independent pixels (6-72).")]
        [DefaultValue(14)]
        public int FontSize { get; set; } = 14;

        /// <summary>Gets or sets the maximum popup width.</summary>
        [Category("Appearance")]
        [DisplayName("Max popup width")]
        [Description("Maximum width of the popup, in device-independent pixels (200-2000).")]
        [DefaultValue(600)]
        public int MaxPopupWidth { get; set; } = 600;

        /// <summary>Gets or sets a value indicating whether the Rasta header is shown in the popup.</summary>
        [Category("Appearance")]
        [DisplayName("Show Rasta header")]
        [Description("Show a small \"Rasta\" label above the comment text.")]
        [DefaultValue(true)]
        public bool ShowHeader { get; set; } = true;

        /// <summary>Creates the immutable, validated snapshot of these options.</summary>
        /// <returns>The settings snapshot.</returns>
        public RastaSettings ToSettings()
        {
            return new RastaSettings(
                Enabled,
                FontFamily,
                FontSize,
                MaxPopupWidth,
                MinimumRtlCharacters,
                ForceRtl,
                MergeConsecutiveLines,
                ShowHeader);
        }
    }
}
