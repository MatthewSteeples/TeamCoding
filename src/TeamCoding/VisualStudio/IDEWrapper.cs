﻿using Microsoft.VisualStudio.Platform.WindowManagement;
using Microsoft.VisualStudio.PlatformUI.Shell.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TeamCoding.SourceControl;
using TeamCoding.VisualStudio.Identity.UserImages;
using TeamCoding.Extensions;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using TeamCoding.Models;

namespace TeamCoding.VisualStudio
{
    public class IDEWrapper
    {
        private Visual _WpfWindow;

        private readonly UserImageCache UserImages = new UserImageCache();
        private Visual WpfWindow => _WpfWindow;
        private readonly EnvDTE.WindowEvents WindowEvents;

        public IDEWrapper(ExternalModelManager remoteModelManager)
        {
            _WpfWindow = GetWpfMainWindow();
            WindowEvents = TeamCodingPackage.Current.DTE.Events.WindowEvents;
            WindowEvents.WindowActivated += WindowEvents_WindowActivated;
            WindowEvents.WindowCreated += WindowEvents_WindowCreated;
        }

        private void WindowEvents_WindowCreated(EnvDTE.Window window)
        {
            InvokeAsync(() =>
            {
                var filePath = window.Document?.FullName;

                if (filePath == null) return;

                var documentTabPanel = GetWpfMainWindow().FindChild<DocumentTabPanel>();
                var titlePanels = documentTabPanel.FindChildren("TitlePanel").Cast<DockPanel>();
                var tabItems = titlePanels.Select(dp => new { TitlePanel = dp, TitleText = dp.FindChild<TabItemTextControl>() }).ToArray();

                var remoteOpenFiles = TeamCodingPackage.Current.RemoteModelManager.GetOpenFiles();

                var tabItemWithFilePath = tabItems.Select(t => new { Item = t, File = (t.TitleText.DataContext as WindowFrameTitle).GetUpdatedTooltip().TrimEnd('*') }).Single(t => t.File == filePath);

                UpdateTabImages(tabItemWithFilePath.Item.TitlePanel, filePath, remoteOpenFiles);
            });
        }

        private void WindowEvents_WindowActivated(EnvDTE.Window gotFocus, EnvDTE.Window lostFocus)
        {
            // TODO: Use this to show which window the user is focused on (delegate to Package.Current.IDEModel)
            var newFilePath = gotFocus?.Document?.FullName;

            if(newFilePath != null)
            {
                TeamCodingPackage.Current.IdeModel.OnFileGotFocus(newFilePath);
            }

            var oldFilePath = lostFocus?.Document?.FullName;

            if (oldFilePath != null)
            {
                TeamCodingPackage.Current.IdeModel.OnFileLostFocus(oldFilePath);
            }
        }
        public DispatcherOperation InvokeAsync(Action callback)
        {
            return _WpfWindow.Dispatcher.InvokeAsync(callback, DispatcherPriority.ContextIdle);
        }
        private Visual GetWpfMainWindow()
        {
            var DTE = TeamCodingPackage.Current.DTE;

            if (DTE == null)
            {
                throw new ArgumentNullException(nameof(DTE));
            }

            var hwndMainWindow = (IntPtr)DTE.MainWindow.HWnd;
            if (hwndMainWindow == IntPtr.Zero)
            {
                throw new NullReferenceException("DTE.MainWindow.HWnd is null.");
            }

            var hwndSource = HwndSource.FromHwnd(hwndMainWindow);
            if (hwndSource == null)
            {
                throw new NullReferenceException("HwndSource for DTE.MainWindow is null.");
            }

            return hwndSource.RootVisual;
        }
        public void UpdateIDE(ExternalModelManager remoteModelManager)
        {
            // TODO: Pass a cancellation token so we can cancel when disposed. Dispose of this in the package dispose method
            WpfWindow.Dispatcher.InvokeAsync(() => UpdateIDE_Internal(remoteModelManager));
        }
        private void UpdateIDE_Internal(ExternalModelManager remoteModelManager)
        {
            remoteModelManager.SyncChanges();

            // TODO: Handle a new tab being opened (see what icons we should add)

            // TODO: Cache this (probably need to re-do cache when closing/opening a solution)
            var documentTabPanel = _WpfWindow.FindChild<DocumentTabPanel>();
            
            if (documentTabPanel == null)
            { // We don't have a doc panel ATM (no docs are open)
                return;
            }
            
            var titlePanels = documentTabPanel.FindChildren("TitlePanel").Cast<DockPanel>();
            var tabItems = titlePanels.Select(dp => new { TitlePanel = dp, TitleText = dp.FindChild<TabItemTextControl>() }).ToArray();

            var remoteOpenFiles = TeamCodingPackage.Current.RemoteModelManager.GetOpenFiles();

            // TODO: Is there a better way to get the tab's full file path than parsing the tooltip? (there must be!)
            var tabItemsWithFilePaths = tabItems.Select(t => new { Item = t, File = (t.TitleText.DataContext as WindowFrameTitle).GetUpdatedTooltip().TrimEnd('*') }).ToArray();

            foreach (var tabItem in tabItemsWithFilePaths)
            {
                UpdateTabImages(tabItem.Item.TitlePanel, tabItem.File, remoteOpenFiles);
            }
        }

        private void UpdateTabImages(DockPanel titlePanel, string filePath, IEnumerable<RemoteDocumentData> remoteOpenFiles)
        {
            var repoInfo = new SourceControlRepo().GetRepoDocInfo(filePath);
            if (repoInfo == null) return;

            var relativePath = repoInfo.RelativePath;

            var remoteDocuments = remoteOpenFiles.Where(rof => repoInfo.RepoUrl == rof.Repository && rof.RelativePath == repoInfo.RelativePath).ToList();

            UpdateOrRemoveImages(titlePanel, remoteDocuments);

            AddImages(titlePanel, remoteDocuments);
        }

        private void UpdateOrRemoveImages(DockPanel tabPanel, List<RemoteDocumentData> remoteDocuments)
        {
            foreach (var border in tabPanel.Children.OfType<Border>().ToArray())
            {
                var imageDocData = (RemoteDocumentData)border.Tag;

                var matchedRemoteDoc = remoteDocuments.SingleOrDefault(rd => rd.RelativePath == imageDocData.RelativePath &&
                                                                             rd.IdeUserIdentity.DisplayName == imageDocData.IdeUserIdentity.DisplayName);

                if (matchedRemoteDoc == null)
                {
                    border.Remove();
                }
                else
                {
                    if (imageDocData.BeingEdited != matchedRemoteDoc.BeingEdited)
                    {
                        imageDocData.BeingEdited = matchedRemoteDoc.BeingEdited;
                        UserImages.SetImageProperties(border, matchedRemoteDoc);
                    }

                    if(imageDocData.HasFocus != matchedRemoteDoc.HasFocus)
                    {
                        imageDocData.HasFocus = matchedRemoteDoc.HasFocus;
                        UserImages.SetImageProperties(border, matchedRemoteDoc);
                    }
                }
            }
        }

        private void AddImages(DockPanel tabPanel, List<RemoteDocumentData> remoteDocuments)
        {
            foreach (var remoteTabItem in remoteDocuments)
            {
                if (!tabPanel.Children.OfType<Border>().Any(i => (i.Tag as RemoteDocumentData).Equals(remoteTabItem)))
                {
                    var imgUser = UserImages.GetUserImageFromUrl(remoteTabItem.IdeUserIdentity.ImageUrl);

                    if (imgUser != null)
                    {
                        imgUser.Width = (tabPanel.Children[0] as GlyphButton).Width;
                        imgUser.Height = (tabPanel.Children[0] as GlyphButton).Height;
                        imgUser.Margin = (tabPanel.Children[0] as GlyphButton).Margin;
                        UserImages.SetImageProperties(imgUser, remoteTabItem);
                        imgUser.Tag = remoteTabItem;

                        tabPanel.Children.Insert(tabPanel.Children.Count, imgUser);
                    }
                }
            }
        }
    }
}
