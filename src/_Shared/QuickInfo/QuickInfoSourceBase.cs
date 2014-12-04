﻿using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using JSON_Intellisense._Shared.Resources;

namespace JSON_Intellisense
{
    public abstract class QuickInfoSourceBase : IQuickInfoSource
    {
        public ITextBuffer _buffer;

        public QuickInfoSourceBase(ITextBuffer subjectBuffer)
        {
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (session == null || qiContent == null)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            JSONEditorDocument doc = JSONEditorDocument.FromTextBuffer(_buffer);
            JSONParseItem item = doc.JSONDocument.ItemBeforePosition(point.Value.Position);

            if (item == null || !item.IsValid)
                return;

            JSONMember dependency = item.FindType<JSONMember>();
            if (dependency == null)
                return;

            var parent = dependency.Parent.FindType<JSONMember>();
            if (parent == null || !parent.UnquotedNameText.EndsWith("dependencies", StringComparison.OrdinalIgnoreCase))
                return;

            applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);

            if (dependency.Value == item)
            {
                string version = item.Text.Trim('"', '~', '^');
                qiContent.Add("   " + version + "  " + Resource.CompletionVersionLatest + Environment.NewLine +
                              "~" + version + "  " + Resource.CompletionVersionMinor + Environment.NewLine +
                              "^" + version + "  " + Resource.CompletionVersionMajor);
            }
            else
            {
                UIElement element = CreateTooltip(dependency.UnquotedNameText, item);

                if (element != null)
                    qiContent.Add(element);
            }
        }

        public abstract UIElement CreateTooltip(string name, JSONParseItem item);

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
