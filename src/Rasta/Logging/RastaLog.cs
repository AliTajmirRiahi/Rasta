using System;
using System.Globalization;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Rasta.Logging
{
    /// <summary>
    /// Writes diagnostics to the Visual Studio Activity Log and to the "Rasta" Output window pane.
    /// Logging never throws and never blocks the caller.
    /// </summary>
    internal static class RastaLog
    {
        private const string Source = "Rasta";
        private const string PaneName = "Rasta";
        private static readonly TimeSpan RepeatWindow = TimeSpan.FromSeconds(10);
        private static readonly object Gate = new object();
        private static readonly AsyncLazy<OutputWindowPane?> Pane =
            new AsyncLazy<OutputWindowPane?>(CreatePaneAsync, ThreadHelper.JoinableTaskFactory);

        private static string lastMessage = string.Empty;
        private static DateTime lastWriteUtc = DateTime.MinValue;

        /// <summary>Logs an informational message.</summary>
        /// <param name="message">The message to log.</param>
        public static void Info(string message)
        {
            ActivityLog.TryLogInformation(Source, message);
            WriteToPane(message);
        }

        /// <summary>Logs an exception that was caught inside Rasta.</summary>
        /// <param name="context">What Rasta was doing when the exception occurred.</param>
        /// <param name="exception">The caught exception.</param>
        public static void Error(string context, Exception exception)
        {
            string message = string.Format(CultureInfo.InvariantCulture, "{0} failed: {1}", context, exception);
            lock (Gate)
            {
                DateTime now = DateTime.UtcNow;
                if (message == lastMessage && now - lastWriteUtc < RepeatWindow)
                {
                    return;
                }

                lastMessage = message;
                lastWriteUtc = now;
            }

            ActivityLog.TryLogError(Source, message);
            WriteToPane("ERROR " + message);
        }

        private static void WriteToPane(string message)
        {
            try
            {
                string line = string.Format(CultureInfo.InvariantCulture, "[{0:HH:mm:ss}] {1}", DateTime.Now, message);
                WriteLineAsync(line).FileAndForget("vs/rasta/log");
            }
            catch (Exception ex)
            {
                ActivityLog.TryLogError(Source, "Could not write to the Rasta output pane: " + ex.Message);
            }
        }

        private static async Task WriteLineAsync(string line)
        {
            OutputWindowPane? pane = await Pane.GetValueAsync().ConfigureAwait(false);
            if (pane != null)
            {
                await pane.WriteLineAsync(line).ConfigureAwait(false);
            }
        }

        private static async Task<OutputWindowPane?> CreatePaneAsync()
        {
            try
            {
                return await VS.Windows.CreateOutputWindowPaneAsync(PaneName);
            }
            catch (Exception ex)
            {
                ActivityLog.TryLogError(Source, "Could not create the Rasta output pane: " + ex.Message);
                return null;
            }
        }
    }
}
