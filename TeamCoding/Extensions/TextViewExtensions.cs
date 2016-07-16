﻿using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamCoding.Extensions
{
    public static class TextViewExtensions
    {
        public static string GetTextDocumentFilePath(this IWpfTextView textView)
        {
            return textView.TextBuffer.GetTextDocumentFilePath();
        }
    }
}