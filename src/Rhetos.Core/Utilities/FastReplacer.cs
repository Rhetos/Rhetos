/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    /// <summary>
    /// FastReplacer is a utility class similar to StringBuilder, with fast Replace function.
    /// FastReplacer is limited to replacing only properly formatted tokens.
    /// Use ToString() function to get the final text.
    /// </summary>
    public class FastReplacer
    {
        private readonly string TokenOpen;
        private readonly string TokenClose;

        /// <summary>
        /// All tokens that will be replaced must have same opening and closing delimiters, such as "{" and "}".
        /// </summary>
        /// <param name="tokenOpen">Opening delimiter for tokens.</param>
        /// <param name="tokenClose">Closing delimiter for tokens.</param>
        /// <param name="caseSensitive">Set caseSensitive to false to use case-insensitive search when replacing tokens.</param>
        public FastReplacer(string tokenOpen, string tokenClose, bool caseSensitive = true)
        {
            if (string.IsNullOrEmpty(tokenOpen) || string.IsNullOrEmpty(tokenClose))
                throw new ArgumentException("Token must have opening and closing delimiters, such as \"{\" and \"}\".");

            TokenOpen = tokenOpen;
            TokenClose = tokenClose;

            var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.InvariantCultureIgnoreCase;
            OccurrencesOfToken = new Dictionary<string, List<TokenOccurrence>>(stringComparer);
        }

        private readonly Dictionary<string, FastReplacerSnippet> RootSnippetByFile = new Dictionary<string, FastReplacerSnippet>();

        private class TokenOccurrence
        {
            public FastReplacerSnippet Snippet;
            public int Start; // Position of a token in the snippet.
            public int End; // Position of a token in the snippet.
        }

        private readonly Dictionary<string, List<TokenOccurrence>> OccurrencesOfToken;

        public void Append(string text)
        {
            AppendToFile(text, string.Empty);
        }

        public void AppendToFile(string text, string path)
        {
            if (!RootSnippetByFile.TryGetValue(path, out FastReplacerSnippet rootSnippet))
            {
                rootSnippet = new FastReplacerSnippet("");
                RootSnippetByFile.Add(path, rootSnippet);
            }

            var snippet = new FastReplacerSnippet(text);
            rootSnippet.Append(snippet);
            ExtractTokens(snippet);
        }

        /// <returns>Returns true if the token was found, false if nothing was replaced.</returns>
        public bool Replace(string token, string text)
        {
            ValidateToken(token, text, false);
            List<TokenOccurrence> occurrences;
            if (OccurrencesOfToken.TryGetValue(token, out occurrences) && occurrences.Count > 0)
            {
                OccurrencesOfToken.Remove(token);
                var snippet = new FastReplacerSnippet(text);
                foreach (var occurrence in occurrences)
                    occurrence.Snippet.Replace(occurrence.Start, occurrence.End, snippet);
                ExtractTokens(snippet);
                return true;
            }
            return false;
        }

        /// <returns>Returns true if the token was found, false if nothing was replaced.</returns>
        public bool InsertBefore(string token, string text)
        {
            ValidateToken(token, text, false);
            List<TokenOccurrence> occurrences;
            if (OccurrencesOfToken.TryGetValue(token, out occurrences) && occurrences.Count > 0)
            {
                var snippet = new FastReplacerSnippet(text);
                foreach (var occurrence in occurrences)
                    occurrence.Snippet.InsertBefore(occurrence.Start, snippet);
                ExtractTokens(snippet);
                return true;
            }
            return false;
        }

        /// <returns>Returns true if the token was found, false if nothing was replaced.</returns>
        public bool InsertAfter(string token, string text, bool reverse = false)
        {
            ValidateToken(token, text, false);
            List<TokenOccurrence> occurrences;
            if (OccurrencesOfToken.TryGetValue(token, out occurrences) && occurrences.Count > 0)
            {
                var snippet = new FastReplacerSnippet(text);
                foreach (var occurrence in occurrences)
                    occurrence.Snippet.InsertAfter(occurrence.End, snippet, reverse);
                ExtractTokens(snippet);
                return true;
            }
            return false;
        }

        public bool Contains(string token)
        {
            ValidateToken(token, token, false);
            List<TokenOccurrence> occurrences;
            if (OccurrencesOfToken.TryGetValue(token, out occurrences))
                return occurrences.Count > 0;
            return false;
        }

        private void ExtractTokens(FastReplacerSnippet snippet)
        {
            int last = 0;
            while (last < snippet.Text.Length)
            {
                // Find next token position in snippet.Text:
                int start = snippet.Text.IndexOf(TokenOpen, last, StringComparison.InvariantCultureIgnoreCase);
                if (start == -1)
                    return;
                int end = snippet.Text.IndexOf(TokenClose, start + TokenOpen.Length, StringComparison.InvariantCultureIgnoreCase);
                if (end == -1)
                    throw new ArgumentException(string.Format("Token is opened but not closed in text \"{0}\".", snippet.Text));
                int eol = snippet.Text.IndexOf('\n', start + TokenOpen.Length);
                if (eol != -1 && eol < end)
                {
                    last = eol + 1;
                    continue;
                }

                // Take the token from snippet.Text:
                end += TokenClose.Length;
                string token = snippet.Text.Substring(start, end - start);
                string context = snippet.Text;
                ValidateToken(token, context, true);

                // Add the token to the dictionary:
                var tokenOccurrence = new TokenOccurrence { Snippet = snippet, Start = start, End = end };
                List<TokenOccurrence> occurrences;
                if (OccurrencesOfToken.TryGetValue(token, out occurrences))
                    occurrences.Add(tokenOccurrence);
                else
                    OccurrencesOfToken.Add(token, new List<TokenOccurrence> { tokenOccurrence });

                last = end;
            }
        }

        private void ValidateToken(string token, string context, bool alreadyValidatedStartAndEnd)
        {
            if (!alreadyValidatedStartAndEnd)
            {
                if (!token.StartsWith(TokenOpen, StringComparison.InvariantCultureIgnoreCase))
                    throw new ArgumentException(string.Format("Token \"{0}\" should start with \"{1}\". Used with text \"{2}\".", token, TokenOpen, context));
                int closePosition = token.IndexOf(TokenClose, StringComparison.InvariantCultureIgnoreCase);
                if (closePosition == -1)
                    throw new ArgumentException(string.Format("Token \"{0}\" should end with \"{1}\". Used with text \"{2}\".", token, TokenClose, context));
                if (closePosition != token.Length - TokenClose.Length)
                    throw new ArgumentException(string.Format("Token \"{0}\" is closed before the end of the token. Used with text \"{1}\".", token, context));
            }

            if (token.Length == TokenOpen.Length + TokenClose.Length)
                throw new ArgumentException(string.Format("Token has no body. Used with text \"{0}\".", context));
            if (token.Contains("\n"))
                throw new ArgumentException(string.Format("Unexpected end-of-line within a token. Used with text \"{0}\".", context));
            if (token.IndexOf(TokenOpen, TokenOpen.Length, StringComparison.InvariantCultureIgnoreCase) != -1)
                throw new ArgumentException(string.Format("Next token is opened before a previous token was closed in token \"{0}\". Used with text \"{1}\".", token, context));
        }

        public IEnumerable<string> GetPaths()
        {
            return RootSnippetByFile.Keys;
        }

        public string ToString(string path)
        {
            return ToString(new[] { RootSnippetByFile[path] });
        }

        public override string ToString()
        {
            return ToString(RootSnippetByFile.Values);
        }

        private static string ToString(IReadOnlyCollection<FastReplacerSnippet> rootSnippets)
        {
            int totalTextLength = rootSnippets.Sum(rootSnippet => rootSnippet.GetLength());
            var sb = new StringBuilder(totalTextLength);
            foreach (var rootSnippet in rootSnippets)
                rootSnippet.ToString(sb);
            if (sb.Length != totalTextLength)
                throw new InvalidOperationException(string.Format(
                    "Internal error: Calculated total text length ({0}) is different from actual ({1}).",
                    totalTextLength, sb.Length));
            return sb.ToString();
        }
    }
}
