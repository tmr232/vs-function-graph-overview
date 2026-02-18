using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace FunctionGraphOverview
{
    internal static class NavigationService
    {
        /// <summary>
        /// Moves the caret in the active text editor to the position corresponding
        /// to the given UTF-8 byte offset and ensures it is visible.
        /// </summary>
        public static void NavigateToByteOffset(int byteOffset)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textManager = (IVsTextManager)
                ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
            if (textManager == null)
                return;

            textManager.GetActiveView(1, null, out IVsTextView activeView);
            if (activeView == null)
                return;

            activeView.GetBuffer(out IVsTextLines buffer);
            if (buffer == null)
                return;

            buffer.GetLastLineIndex(out int lastLine, out int lastCol);
            buffer.GetLineText(0, 0, lastLine, lastCol, out string allText);
            if (allText == null)
                return;

            int charOffset = Utf8ByteOffsetToCharOffset(allText, byteOffset);
            if (charOffset < 0)
                return;

            buffer.GetLineIndexOfPosition(charOffset, out int line, out int col);

            activeView.SetCaretPos(line, col);
            activeView.CenterLines(line, 1);
        }

        internal static int Utf8ByteOffsetToCharOffset(string text, int byteOffset)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            if (byteOffset < 0 || byteOffset > bytes.Length)
                return -1;

            // Decode exactly byteOffset bytes back to a string and return its length.
            // This gives us the number of UTF-16 chars that correspond to those bytes.
            return Encoding.UTF8.GetCharCount(bytes, 0, byteOffset);
        }
    }
}
