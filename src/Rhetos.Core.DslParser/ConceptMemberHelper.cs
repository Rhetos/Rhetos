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

using System.Text;

namespace Rhetos.Dsl
{
    public static class ConceptMemberHelper
    {
        public static void AppendWithQuotesIfNeeded(StringBuilder text, string s)
        {
            bool clean = true;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_'))
                {
                    clean = false;
                    break;
                }
            }
            if (clean && s.Length > 0)
                text.Append(s);
            else
            {
                string quote = (s.Contains("\'") && !s.Contains("\"")) ? "\"" : "\'";
                text.Append(quote).Append(s.Replace(quote, quote + quote)).Append(quote);
            }
        }
    }
}