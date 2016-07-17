﻿using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCoding.Extensions;
using TeamCoding.SourceControl;
using Microsoft.VisualStudio.Text;

namespace TeamCoding.VisualStudio.Models.Local
{
    /// <summary>
    /// Represents and maintains a model of the IDE, firing change events as appropriate
    /// </summary>
    public class LocalIDEModel
    {
        public class FileFocusChangedEventArgs : EventArgs
        {
            public string LostFocusFile { get; set; }
            public string GotFocusFile { get; set; }
        }

        private static LocalIDEModel Current = new LocalIDEModel();

        private object OpenFilesLock = new object();
        private readonly ConcurrentDictionary<string, SourceControlRepo.RepoDocInfo> OpenFiles = new ConcurrentDictionary<string, SourceControlRepo.RepoDocInfo>();

        public event EventHandler OpenViewsChanged;
        public event EventHandler<TextContentChangedEventArgs> TextContentChanged;
        public event EventHandler<TextDocumentFileActionEventArgs> TextDocumentSaved;

        public void OnOpenedTextView(IWpfTextView view)
        {
            var filePath = view.GetTextDocumentFilePath();
            var sourceControlInfo = TeamCodingPackage.Current.SourceControlRepo.GetRepoDocInfo(filePath);

            if (sourceControlInfo != null)
            {
                lock (OpenFilesLock)
                {
                    if (!OpenFiles.ContainsKey(filePath))
                    {
                        // TODO: maybe use https://msdn.microsoft.com/en-us/library/envdte.sourcecontrol.aspx to check if it's in source control

                        OpenFiles.AddOrUpdate(filePath, sourceControlInfo, (v, e) => e);
                    }
                }
                OpenViewsChanged?.Invoke(this, new EventArgs());
            }
        }

        internal void OnTextDocumentDisposed(ITextDocument textDocument, TextDocumentEventArgs e)
        {
            SourceControlRepo.RepoDocInfo tmp;
            lock (OpenFilesLock)
            {
                OpenFiles.TryRemove(textDocument.FilePath, out tmp);
            }
            OpenViewsChanged?.Invoke(this, new EventArgs());
        }

        internal void OnTextDocumentSaved(ITextDocument textDocument, TextDocumentFileActionEventArgs e)
        {
            var sourceControlInfo = TeamCodingPackage.Current.SourceControlRepo.GetRepoDocInfo(textDocument.FilePath);
            if (sourceControlInfo != null)
            {
                lock (OpenFilesLock)
                {
                    OpenFiles.AddOrUpdate(textDocument.FilePath, sourceControlInfo, (v, r) => sourceControlInfo);
                }

                TextDocumentSaved?.Invoke(textDocument, e);
            }
        }

        public SourceControlRepo.RepoDocInfo[] OpenDocs()
        {
            lock (OpenFilesLock)
            {
                return OpenFiles.Values.ToArray();
            }
        }

        internal void OnTextBufferChanged(ITextBuffer textBuffer, TextContentChangedEventArgs e)
        {
            var filePath = textBuffer.GetTextDocumentFilePath();
            var sourceControlInfo = TeamCodingPackage.Current.SourceControlRepo.GetRepoDocInfo(filePath);

            lock (OpenFilesLock)
            {
                if (sourceControlInfo == null)
                {
                    // The file could have just been put on the ignore list, so remove it from the list
                    OpenFiles.TryRemove(filePath, out sourceControlInfo);
                }
                else
                {
                    OpenFiles.AddOrUpdate(filePath, sourceControlInfo, (v, r) => sourceControlInfo);
                }
            }
            
            TextContentChanged?.Invoke(textBuffer, e);
        }

        internal void OnFileGotFocus(string filePath)
        {
            // Update this source control info to update the time
            var sourceControlInfo = TeamCodingPackage.Current.SourceControlRepo.GetRepoDocInfo(filePath);
            if (sourceControlInfo != null)
            {
                lock (OpenFilesLock)
                {
                    OpenFiles.AddOrUpdate(filePath, sourceControlInfo, (v, r) => sourceControlInfo);
                }
                OpenViewsChanged?.Invoke(this, new EventArgs());
            }
        }

        internal void OnFileLostFocus(string filePath)
        {
        }
    }
}
