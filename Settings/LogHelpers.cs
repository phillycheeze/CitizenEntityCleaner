// Settings/LogHelpers.cs
namespace CitizenEntityCleaner
{
    using System;                         // Exception
    using System.Diagnostics;             // Process, ProcessStartInfo
    using System.IO;                      // File, Directory, Path, FileStream
    using System.Text;                    // Encoding
    using System.Text.RegularExpressions; // Regex for trimming only the timestamp
    using Colossal.Logging;               // ILog
    using Game.UI.Localization;

    /// <summary>
    /// Helpers for the Debug tab: live log viewer + Open Log button.
    /// Trick the Settings UI by returning a LocalizedString whose Id is the
    /// actual text we want to display. If there's no matching localization entry,
    /// the widget fallsback to show the Id literally — i.e., our log text.
    ///
    /// To ensure UI refreshes even when new lines are *visually identical*
    /// (because timestamps trimmed, it lost unique), need to append an invisible suffix to the Id
    /// that changes whenever the log file grows. Zero-width spaces are used so
    /// nothing visible changes for the player.
    /// </summary>
    internal static class LogHelpers
    {
        // Manual bump that you can trigger (e.g., from a button) to force refresh.
        // Also use file length which is usually enough on its own.
        private static int s_manualBump = 0;

        /// <summary>
        /// The property used by the Settings UI:
        ///     [SettingsUIDisplayName(typeof(LogHelpers), nameof(LogHelpers.LogText))]
        ///
        /// Compute the trimmed log tail, then build a LocalizedString whose Id is:
        ///     "<trimmed log text>" + "<some invisible zero-width spaces>"
        ///
        /// The invisible suffix changes when the file length changes (and when
        /// RefreshLiveTextNow() is called), which makes the Id different and forces
        /// the Settings widget to re-render, even if the visible text didn't change.
        /// </summary>
        public static LocalizedString LogText
        {
            get
            {
                string display = GetLogTail(); // read + trim timestamps

                // Build an invisible "nonce" based on current file length (+ optional manual bump).
                // This makes the LocalizedString.Id different whenever new bytes are appended,
                // even if the visible content is identical after trimming the timestamps.
                int zCount = 1; // minimum one zero-width char to keep an Id suffix present
                try
                {
                    var fi = new FileInfo(Mod.LogFilePath);
                    if (fi.Exists)
                    {
                        long sig = fi.Length + s_manualBump; // include manual bump
                        zCount = 1 + (int)(sig % 7);         // 1..7 zero-width spaces
                    }
                }
                catch
                {
                    // If we can't stat the file, just leave zCount at 1.
                }

                string invisibleSuffix = new string('\u200B', zCount); // U+200B ZERO WIDTH SPACE
                string id = display + invisibleSuffix;

                // Return a "localized" string by Id; because there is no such key,
                // the UI prints the Id literally (i.e., our display string).
                return LocalizedString.Id(id);
            }
        }

        /// <summary>
        /// Optional helper you can call when you *know* you just wrote to the log
        /// and want to force a refresh even before the file length changes.
        /// </summary>
        public static void RefreshLiveTextNow()
        {
            unchecked { s_manualBump++; }
            // No other work needed; the next time the UI asks for LogText, the Id changes.
        }

        // ---- Tail reader (timestamp-only trimming; keep [INFO]/[WARN]) -----
        /// <summary>
        /// Read and return the tail of our mod log (kept small for UI rendering).
        /// Safe: shares the file with the game logger; won’t throw if file is in use.
        /// Trims ONLY the leading timestamp like "[2025-10-04 16:21:30,469] ",
        /// keeping the level tag "[INFO]/[WARN]/...".
        /// </summary>
        private static string GetLogTail()
        {
            const int kMaxTailBytes = 64 * 1024; // ~64 KB max displayed

            try
            {
                var path = Mod.LogFilePath;
                if (string.IsNullOrWhiteSpace(path))
                    return "[Log] Path not available.";

                if (!File.Exists(path))
                    return "No log file yet. Use the mod, then click [Open Log] or [Refresh Counts].";

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long len = fs.Length;
                    int toRead = (int)Math.Min(len, kMaxTailBytes);
                    fs.Seek(len - toRead, SeekOrigin.Begin);

                    var buffer = new byte[toRead];
                    int read = fs.Read(buffer, 0, toRead);

                    string text = Encoding.UTF8.GetString(buffer, 0, read);

                    // Normalize CRLF for the game's UI.
                    text = text.Replace("\r\n", "\n");

                    // Trim ONLY the timestamp; keep the level box like [INFO], [WARN], etc.
                    text = Regex.Replace(
                        text,
                        @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}\]\s*",
                        "",
                        RegexOptions.Multiline
                    );

                    if (toRead < len)
                        text = "… (tail)\n" + text;

                    return text;
                }
            }
            catch (Exception ex)
            {
                return $"[Log] Failed to read: {ex.GetType().Name}: {ex.Message}";
            }
        }

        // ---- Button helper: open log file or folder ------------------------
        /// <summary>
        /// Opens the log file if it exists; otherwise opens the Logs folder.
        /// Safe: no crash if the file/folder are missing or the shell fails.
        /// Returns true if something was opened successfully.
        /// </summary>
        public static bool TryOpenLogOrFolder(string logPath, ILog? logger = null)
        {
            // Defensive guard in case caller ever passes an empty string.
            if (string.IsNullOrWhiteSpace(logPath))
            {
                logger?.Warn("[Log] TryOpenLogOrFolder called with empty logPath.");
                return false;
            }

            string? dir = Path.GetDirectoryName(logPath);

            try
            {
                if (File.Exists(logPath))
                {
                    var psi = new ProcessStartInfo(logPath)
                    {
                        UseShellExecute = true,   // open via default app
                        ErrorDialog = false,      // avoid OS modal dialogs
                        Verb = "open"
                    };

                    var p = Process.Start(psi);
                    // With UseShellExecute=true the shell may reuse an existing instance.
                    if (p == null)
                    {
                        logger?.Debug("[Log] Shell returned no process handle (likely reused existing app/Explorer). Treating as success.");
                        return true;
                    }
                    return true;
                }

                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    var psi2 = new ProcessStartInfo(dir)
                    {
                        UseShellExecute = true,
                        ErrorDialog = false,
                        Verb = "open"
                    };

                    var p2 = Process.Start(psi2);
                    if (p2 == null)
                    {
                        // Explorer may reuse an existing window and return null — that's okay.
                        logger?.Debug("[Log] Shell returned no process handle when opening the Logs folder (likely reused Explorer). Treating as success.");
                        return true;
                    }
                    return true;
                }

                // Nothing to open: both the file and the folder are missing
                logger?.Info("[Log] No log file yet, and Logs folder not found.");
            }
            catch (Exception ex)
            {
                // Single catch safely handles Win32Exception and any other surprises (no crash)
                logger?.Warn($"[Log] Failed to open path: {ex.GetType().Name}: {ex.Message}");
            }

            return false;
        }
    }
}
