using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class LoggingUtility
    {
        private static Regex ColumnNamesRegex = new Regex(@"^<PREVIOUS(\s+(\w+)="".*?(""|$))*");

        public static string GetSummary(string action, string description)
        {
            string summary = action;
            if (action == "Update" && description != null)
            {
                var columns = new List<string>();
                foreach (var columnCapture in ColumnNamesRegex.Match(description).Groups[2].Captures)
                    columns.Add(((System.Text.RegularExpressions.Capture)columnCapture).Value);
                if (columns.Count() > 0)
                    summary += ": " + string.Join(", ", columns) + ".";
            }
            if (summary.Length > 0 && summary.Last() != '.')
                summary += ".";
            return summary;
        }

        private class ReconstructedUserInfo : IUserInfo
        {
            public bool IsUserRecognized { get; set; }
            public string UserName { get; set; }
            public string Workstation { get; set; }
        }

        public static IUserInfo ExtractUserInfo(string contextInfo)
        {
            if (contextInfo == null)
                return new ReconstructedUserInfo { IsUserRecognized = false, UserName = null, Workstation = null };

            string prefix1 = "Rhetos:";
            string prefix2 = "Alpha:";

            int positionUser;
            if (contextInfo.StartsWith(prefix1))
                positionUser = prefix1.Length;
            else if (contextInfo.StartsWith(prefix2))
                positionUser = prefix2.Length;
            else
                return new ReconstructedUserInfo { IsUserRecognized = false, UserName = null, Workstation = null };

            var result = new ReconstructedUserInfo();

            int positionWorkstation = contextInfo.IndexOf(',', positionUser);
            if (positionWorkstation > -1)
            {
                result.UserName = contextInfo.Substring(positionUser, positionWorkstation - positionUser);
                result.Workstation = contextInfo.Substring(positionWorkstation + 1);
            }
            else
            {
                result.UserName = contextInfo.Substring(positionUser);
                result.Workstation = "";
            }

            result.UserName = result.UserName.Trim();
            if (result.UserName == "") result.UserName = null;
            result.Workstation = result.Workstation.Trim();
            if (result.Workstation == "") result.Workstation = null;

            result.IsUserRecognized = result.UserName != null;
            return result;
        }
    }
}
