using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace NoMixedLineEndings
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(NoMixedLineEndingsPackage.PackageGuidString)]
    public sealed class NoMixedLineEndingsPackage : AsyncPackage
    {
        /// <summary>
        /// NoMixedLineEndingsPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f7ad06be-3a7d-4a39-8db9-ae505b7271f0";


        //static void Log(string s)
        //{
        //    File.AppendAllText("d:\\vs.log", s + "\n");
        //}


        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            //Log("step 1");
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            //Log("step 2");
            var dte = (DTE)await GetServiceAsync(typeof(DTE));

            //Log("step 3");
            var txtMgr = (IVsTextManager)await GetServiceAsync(typeof(SVsTextManager));

            //Log("step 4");
            var runningDocumentTable = new RunningDocumentTable(this);

            //Log("step 5");
            var plugin = new FormatDocumentOnBeforeSave(dte, runningDocumentTable, txtMgr);

            //Log("step 6");
            runningDocumentTable.Advise(plugin);

            //Log("step 7");
        }

        #endregion
    }


#if false
    internal class FormatDocumentOnBeforeSave : IVsRunningDocTableEvents3
    {
        private DTE _dte;
        private IVsTextManager _txtMngr;
        private RunningDocumentTable _runningDocumentTable;

        public FormatDocumentOnBeforeSave(DTE dte, RunningDocumentTable runningDocumentTable, IVsTextManager txtMngr)
        {
            _runningDocumentTable = runningDocumentTable;
            _txtMngr = txtMngr;
            _dte = dte;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
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
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        Document FindDocument(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var documentInfo = _runningDocumentTable.GetDocumentInfo(docCookie);
            var documentPath = documentInfo.Moniker;

            return _dte.Documents.Cast<Document>().FirstOrDefault(doc =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return doc.FullName == documentPath;
            });
        }

        static TextDocument GetTextDocument(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (TextDocument)document.Object("TextDocument");
        }

        static string GetDocumentText(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textDocument = GetTextDocument(document);
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            var content = editPoint.GetText(textDocument.EndPoint);
            return content;
        }

        public int OnBeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var document = FindDocument(docCookie);

            //if no document - do nothing
            if (document == null)
                return VSConstants.S_OK;

            //reading
            string original_text = GetDocumentText(document);

            //is there a mixture of \r\n and \n but no lone \r?
            int count_rn = 0, count_n = 0, count_r = 0;
            for (int i = 0; i < original_text.Length; i++)
            {
                switch (original_text[i])
                {
                    case '\r':
                        count_r += 1;
                        break;

                    case '\n':
                        count_n += 1;
                        if (i >= 1 && original_text[i - 1] == '\r')
                            count_rn += 1;
                        break;
                }
            }
            if (count_rn == 0)      // there are no \r\n at all
                return VSConstants.S_OK;
            if (count_rn == count_n)    // all \n are part of \r\n
                return VSConstants.S_OK;
            if (count_r != count_rn)    // there is some \r that is not part of \r\n
                return VSConstants.S_OK;

            // now we know there is no lone \r, and both \r\n and \n.
            // remove all the '\r'.  It converts the '\r\n' to '\n' while not touching the '\n'
            TextDocument textDocument = GetTextDocument(document);
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            EditPoint endPoint = textDocument.EndPoint.CreateEditPoint();
            editPoint.ReplacePattern(endPoint, @"\r", "");

            return VSConstants.S_OK;
        }
    }
#endif


    internal class FormatDocumentOnBeforeSave : IVsRunningDocTableEvents3
    {
        private DTE _dte;
        private IVsTextManager _txtMngr;
        private RunningDocumentTable _runningDocumentTable;

        public FormatDocumentOnBeforeSave(DTE dte, RunningDocumentTable runningDocumentTable, IVsTextManager txtMngr)
        {
            _runningDocumentTable = runningDocumentTable;
            _txtMngr = txtMngr;
            _dte = dte;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
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
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        Document FindDocument(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var documentInfo = _runningDocumentTable.GetDocumentInfo(docCookie);
            var documentPath = documentInfo.Moniker;

            return _dte.Documents.Cast<Document>().FirstOrDefault(doc =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return doc.FullName == documentPath;
            });
        }

        static string GetDocumentText(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textDocument = (TextDocument)document.Object("TextDocument");
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            var content = editPoint.GetText(textDocument.EndPoint);
            return content;
        }

        static void SetDocumentText(Document document, string content)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textDocument = (TextDocument)document.Object("TextDocument");
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            EditPoint endPoint = textDocument.EndPoint.CreateEditPoint();
            editPoint.ReplaceText(endPoint, content, 0);
        }

        struct scrollData
        {
            public int piMinUnit;
            public int piMaxUnit;
            public int piVisibleUnits;
            public int piFirstVisible;
        }

        struct breakPointData
        {
            public string functionName;
            public int fileLine;
            public int fileColumn;
            public string condition;
            public dbgBreakpointConditionType conditionType;
            public dbgHitCountType hitCountType;
            public string language;
            public int hitCount;
        }

        static readonly bool TRIM_END_OF_LINES_TOO = false;

        public int OnBeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var document = FindDocument(docCookie);

            //if no document - do nothing
            if (document == null)
                return VSConstants.S_OK;

            //preserving cursor position
            IVsTextView textViewCurrent;
            _txtMngr.GetActiveView(1, null, out textViewCurrent);
            int line = 0;
            int column = 0;
            textViewCurrent.GetCaretPos(out line, out column);

            //preserving breakpoints
            List<breakPointData> breakPointsPreserved = new List<breakPointData>();
            foreach (Breakpoint breakPoint in _dte.Debugger.Breakpoints)
            {
                if (breakPoint.File == _dte.ActiveDocument.FullName)
                {
                    breakPointData newBreakPoint = new breakPointData();

                    newBreakPoint.fileColumn = breakPoint.FileColumn;
                    newBreakPoint.fileLine = breakPoint.FileLine;
                    newBreakPoint.language = breakPoint.Language;
                    newBreakPoint.condition = breakPoint.Condition;
                    newBreakPoint.functionName = breakPoint.FunctionName;
                    newBreakPoint.conditionType = breakPoint.ConditionType;

                    newBreakPoint.hitCount = breakPoint.CurrentHits;
                    newBreakPoint.hitCountType = breakPoint.HitCountType;

                    breakPointsPreserved.Add(newBreakPoint);
                }
            }



            //preserving scroll position
            scrollData horizontal = new scrollData();
            scrollData vertical = new scrollData();
            textViewCurrent.GetScrollInfo(0, out horizontal.piMinUnit, out horizontal.piMaxUnit, out horizontal.piVisibleUnits, out horizontal.piFirstVisible);
            textViewCurrent.GetScrollInfo(1, out vertical.piMinUnit, out vertical.piMaxUnit, out vertical.piVisibleUnits, out vertical.piFirstVisible);

            //reading
            string text = GetDocumentText(document);
            string original_text = text;

            //find the first end of line of the document
            var match = Regex.Match(text, @"\r\n|\n|\r",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
            if (!match.Success)
                return VSConstants.S_OK;    //no EOL at all found, don't change anything
            string endLineSymbol = match.Value;

            //replace all ends of lines with that
            text = Regex.Replace(text,
                TRIM_END_OF_LINES_TOO ? @"[ \t]*(\r\n|\n|\r)" : @"\r\n|\n|\r",
                endLineSymbol,
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

            if (TRIM_END_OF_LINES_TOO)
            {
                //remove any trailing spaces and tabs
                text = text.TrimEnd(' ', '\t');
                //let's also ensure the last line ends in a newline
                if (!text.EndsWith(endLineSymbol))
                    text += endLineSymbol;
            }

            //set modified text
            if (text == original_text)
                return VSConstants.S_OK;
            SetDocumentText(document, text);

            //restoring breakpoints
            foreach (breakPointData breakPoint in breakPointsPreserved)
            {
                _dte.Debugger.Breakpoints.Add("", Path.GetFileName(_dte.ActiveDocument.FullName), breakPoint.fileLine, breakPoint.fileColumn, breakPoint.condition, breakPoint.conditionType, breakPoint.language, "", 1, "", breakPoint.hitCount, breakPoint.hitCountType);
            }

            //restoring cursor position
            textViewCurrent.SetCaretPos(line, column);

            //restoring scroll
            textViewCurrent.SetScrollPosition(0, horizontal.piFirstVisible);
            textViewCurrent.SetScrollPosition(1, vertical.piFirstVisible);

            return VSConstants.S_OK;
        }
    }
}
