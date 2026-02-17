using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace FunctionGraphOverview
{
    internal sealed class EditorMonitor : IDisposable, IVsRunningDocTableEvents
    {
        private static readonly Dictionary<string, string> LanguageMap = new Dictionary<
            string,
            string
        >(StringComparer.OrdinalIgnoreCase)
        {
            { ".c", "C" },
            { ".cpp", "C++" },
            { ".cxx", "C++" },
            { ".cc", "C++" },
            { ".h", "C++" },
            { ".hpp", "C++" },
            { ".go", "Go" },
            { ".java", "Java" },
            { ".py", "Python" },
            { ".ts", "TypeScript" },
            { ".tsx", "TSX" },
        };

        private readonly WebviewBridge _bridge;
        private readonly IVsTextManager _textManager;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly IVsRunningDocumentTable _rdt;
        private readonly uint _rdtCookie;
        private readonly DispatcherTimer _debounceTimer;

        private IWpfTextView _currentView;
        private bool _disposed;

        public EditorMonitor(WebviewBridge bridge)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _bridge = bridge;

            _textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

            var oleServiceProvider = (IServiceProvider)
                Package.GetGlobalService(typeof(IServiceProvider));
            var shell = (IVsShell)Package.GetGlobalService(typeof(SVsShell));

            // Get IVsEditorAdaptersFactoryService via the component model
            var componentModel = (Microsoft.VisualStudio.ComponentModelHost.IComponentModel)
                Package.GetGlobalService(
                    typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)
                );
            _editorAdaptersFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            _rdt = (IVsRunningDocumentTable)
                Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            _rdt.AdviseRunningDocTableEvents(this, out _rdtCookie);

            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _debounceTimer.Tick += OnDebounceTimerTick;

            AttachToActiveView();
        }

        private void AttachToActiveView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_textManager == null)
                return;

            _textManager.GetActiveView(1, null, out var vsTextView);
            if (vsTextView == null)
                return;

            var wpfView = _editorAdaptersFactory.GetWpfTextView(vsTextView);
            if (wpfView == null || wpfView == _currentView)
                return;

            DetachFromCurrentView();

            _currentView = wpfView;
            _currentView.Caret.PositionChanged += OnCaretPositionChanged;
            _currentView.TextBuffer.Changed += OnTextBufferChanged;
            _currentView.Closed += OnViewClosed;

            ScheduleSend();
        }

        private void DetachFromCurrentView()
        {
            if (_currentView == null)
                return;

            _currentView.Caret.PositionChanged -= OnCaretPositionChanged;
            _currentView.TextBuffer.Changed -= OnTextBufferChanged;
            _currentView.Closed -= OnViewClosed;
            _currentView = null;
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            DetachFromCurrentView();
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            ScheduleSend();
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ScheduleSend();
        }

        private void ScheduleSend()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnDebounceTimerTick(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _debounceTimer.Stop();
            SendCurrentState();
        }

        private void SendCurrentState()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_currentView == null || _currentView.IsClosed)
                return;

            var document =
                _currentView.TextDataModel?.DocumentBuffer?.Properties.GetProperty<ITextDocument>(
                    typeof(ITextDocument)
                );
            if (document == null)
                return;

            var filePath = document.FilePath;
            var ext = Path.GetExtension(filePath);
            if (!LanguageMap.TryGetValue(ext, out var language))
                return;

            var snapshot = _currentView.TextSnapshot;
            var text = snapshot.GetText();

            var caretPosition = _currentView.Caret.Position.BufferPosition;
            var textBeforeCaret = snapshot.GetText(0, caretPosition.Position);
            var byteOffset = Encoding.UTF8.GetByteCount(textBeforeCaret);

            // TODO: Actually await the task!
            ThreadHelper.JoinableTaskFactory.Run(
                async delegate
                {
                    _ = _bridge.SendCodeAsync(text, byteOffset, language);
                }
            );
        }

        #region IVsRunningDocTableEvents

        public int OnAfterFirstDocumentLock(
            uint docCookie,
            uint dwRDTLockType,
            uint dwReadLocksRemaining,
            uint dwEditLocksRemaining
        )
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(
            uint docCookie,
            uint dwRDTLockType,
            uint dwReadLocksRemaining,
            uint dwEditLocksRemaining
        )
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            AttachToActiveView();
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        #endregion

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _debounceTimer.Stop();
            _debounceTimer.Tick -= OnDebounceTimerTick;

            DetachFromCurrentView();

            ThreadHelper.ThrowIfNotOnUIThread();
            if (_rdt != null && _rdtCookie != 0)
            {
                _rdt.UnadviseRunningDocTableEvents(_rdtCookie);
            }
        }
    }
}
