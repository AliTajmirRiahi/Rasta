using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Rasta.Logging;
using Rasta.Options;
using Task = System.Threading.Tasks.Task;

namespace Rasta
{
    /// <summary>Identity constants shared by the package and its registration attributes.</summary>
    internal static class PackageGuids
    {
        /// <summary>The package GUID as a string.</summary>
        public const string RastaString = "0127e156-0ddb-4146-a838-ed25a71a8186";
    }

    /// <summary>
    /// The Rasta package. It loads in the background, registers the options page and keeps the
    /// <see cref="RastaSettings"/> snapshot in sync with the user's options.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Rasta – Persian RTL Comments", "Shows Persian/Arabic code comments mixed with English correctly (RTL) in a hover popup.", "1.0.0")]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptionsPage), "Rasta", "General", 0, 0, true, SupportsProfiles = true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuids.RastaString)]
    public sealed class RastaPackage : ToolkitPackage
    {
        /// <inheritdoc />
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            GeneralOptions options = await GeneralOptions.GetLiveInstanceAsync();
            RastaSettings.Publish(options.ToSettings());
            GeneralOptions.Saved += OnOptionsSaved;
            RastaLog.Info("Rasta loaded.");
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GeneralOptions.Saved -= OnOptionsSaved;
            }

            base.Dispose(disposing);
        }

        private static void OnOptionsSaved(GeneralOptions options)
        {
            RastaSettings.Publish(options.ToSettings());
        }
    }
}
