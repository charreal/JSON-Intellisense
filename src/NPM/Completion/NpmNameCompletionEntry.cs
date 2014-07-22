﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using EnvDTE80;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Newtonsoft.Json.Linq;

namespace JSON_Intellisense.NPM
{
    class NpmNameCompletionEntry : JSONCompletionEntry
    {
        private DTE2 _dte;
        private JSONDocument _doc;
        internal static IEnumerable<string> _searchResults;

        public NpmNameCompletionEntry(string text, IIntellisenseSession session, DTE2 dte, JSONDocument doc)
            : base(text, "\"" + text + "\"", null, Constants.Icon, null, false, session as ICompletionSession)
        {
            _dte = dte;
            _doc = doc;
        }

        public override void Commit()
        {
            if (base.DisplayText != "Search NPM...")
            {
                base.Commit();
            }
            else
            {
                string searchTerm = FindSearchTerm();

                if (string.IsNullOrEmpty(searchTerm))
                    return;

                ExecuteSearch(searchTerm);
            }
        }

        private void ExecuteSearch(string searchTerm)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                string url = string.Format(Constants.SearchUrl, HttpUtility.UrlEncode(searchTerm));
                string result = Helper.DownloadText(_dte, url);
                var children = GetChildren(result);

                if (children.Count() == 0)
                {
                    _dte.StatusBar.Text = "No packages found matching '" + searchTerm + "'";
                    base.Session.Dismiss();
                    return;
                }

                _dte.StatusBar.Text = string.Empty;
                _searchResults = children.Select(c => (string)c["value"]);

                Helper.ExecuteCommand(_dte, "Edit.ListMembers");
            });
        }

        private static JEnumerable<JToken> GetChildren(string result)
        {
            try
            {
                JArray array = JArray.Parse(result);
                return array.Children();
            }
            catch
            { }

            return JEnumerable<JToken>.Empty;
        }

        private string FindSearchTerm()
        {
            try
            {
                JSONParseItem member = _doc.ItemBeforePosition(base.Session.TextView.Caret.Position.BufferPosition.Position);
                return member.Text.Trim('"');
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}