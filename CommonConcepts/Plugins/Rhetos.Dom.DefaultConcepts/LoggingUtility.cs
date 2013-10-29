using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class LoggingUtility
    {
        private static Regex ColumnNamesRegex = new Regex(@"^<PREVIOUS(\s+(\w+)="".*?(""|$))*");

        public static string GetSummary(string action, string description)
        {
            string summary = "";
            if (action == "Update" && description != null)
            {
                var columns = new List<string>();
                foreach (var columnCapture in ColumnNamesRegex.Match(description).Groups[2].Captures)
                    columns.Add(((System.Text.RegularExpressions.Capture)columnCapture).Value);
                if (columns.Count() > 0)
                    summary = string.Join(", ", columns);
            }
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

        public class EventDescription
        {
            public Guid LogID;
            public string Action;
            public string Description;
        }

        public class ReconstructedDataModifications
        {
            public Guid LogID;
            public string Property;
            public string OldValue;
            public string NewValue;
            public bool Modified;
        }

        public static List<ReconstructedDataModifications> ReconstructDataModifications(List<EventDescription> eventDescriptions, string currentItemDescription)
        {
            var allEvents = new[] { new EventDescription { LogID = Guid.Empty, Action = "Current", Description = currentItemDescription } }
                .Concat(eventDescriptions)
                .ToList();

            // Extract all property changes for each event:

            var eventsChanges = allEvents.Select<EventDescription, ValueOrError<Dictionary<string, string>>>(e =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(e.Description))
                            return new Dictionary<string, string>();

                        const string errorDescription = "Error: ";
                        if (e.Description.StartsWith(errorDescription))
                            return ValueOrError.CreateError(e.Description.Substring(errorDescription.Length));

                        var xml = XElement.Parse(e.Description);

                        if (xml.Name.LocalName != "PREVIOUS")
                            return ValueOrError.CreateError("Invalid event Description format"); // TODO: Log the error (ILogger needs to be added to ExecutionContext).

                        return xml.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);
                    }
                    catch
                    {
                        return ValueOrError.CreateError("Invalid event Description format"); // TODO: Log the exception (ILogger needs to be added to ExecutionContext).
                    }
                }).ToList();

            var allProperties = new HashSet<string>(eventsChanges.Where(ec => !ec.IsError).SelectMany(ec => ec.Value.Select(p => p.Key)));

            // Accumulate the changes to reconstruct value for each property at each event:

            var eventsProperties = new List<Dictionary<string, string>>(allEvents.Count());
            var lastEventProperties = allProperties.ToDictionary(p => p, p => "");

            for (int i = 0; i < allEvents.Count(); i++)
            {
                if (allEvents[i].Action == "Insert")
                    eventsChanges[i] = lastEventProperties.Where(lep => lep.Value != "").ToDictionary(lep => lep.Key, lep => "");
                else if (eventsChanges[i].IsError)
                    eventsChanges[i] = allProperties.ToDictionary(p => p, p => "<" + eventsChanges[i].Error + ">");

                if (allEvents[i].Action == "Delete")
                    foreach (var p in lastEventProperties.Keys.ToArray())
                        if (lastEventProperties[p].StartsWith("<"))
                            lastEventProperties[p] = "";

                var eventProperties = new Dictionary<string, string>(lastEventProperties);

                foreach (var change in eventsChanges[i].Value)
                    eventProperties[change.Key] = change.Value;

                eventsProperties.Add(eventProperties);
                lastEventProperties = eventProperties;
            }

            // Compose the result list:

            var result = Zip4(
                eventDescriptions,
                eventsChanges.Skip(1),
                eventsProperties.Skip(1),
                eventsProperties.Take(allEvents.Count() - 1),

                (eventDescription, changes, propertiesOld, propertiesNew) =>
                    propertiesOld.OrderBy(p => p.Key)
                        .Select(p => new ReconstructedDataModifications
                            {
                                LogID = eventDescription.LogID,
                                Property = p.Key,
                                OldValue = p.Value,
                                NewValue = propertiesNew[p.Key],
                                Modified = changes.Value.ContainsKey(p.Key)
                            }));

            return result.SelectMany(properties => properties).ToList();
        }

        private static IEnumerable<TResult> Zip4<T1, T2, T3, T4, TResult>(
            IEnumerable<T1> list1, IEnumerable<T2> list2, IEnumerable<T3> list3, IEnumerable<T4> list4,
            Func<T1, T2, T3, T4, TResult> resultSelector)
        {
            if (list2.Count() != list1.Count()) throw new ArgumentException("Zip4 list2 (count " + list2.Count() + ") is not the same length as list1 (count " + list1.Count() + ").");
            if (list3.Count() != list1.Count()) throw new ArgumentException("Zip4 list3 (count " + list3.Count() + ") is not the same length as list1 (count " + list1.Count() + ").");
            if (list4.Count() != list1.Count()) throw new ArgumentException("Zip4 list4 (count " + list4.Count() + ") is not the same length as list1 (count " + list1.Count() + ").");

            using (var e1 = list1.GetEnumerator())
            using (var e2 = list2.GetEnumerator())
            using (var e3 = list3.GetEnumerator())
            using (var e4 = list4.GetEnumerator())
                while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext() && e4.MoveNext())
                    yield return resultSelector(e1. Current, e2.Current, e3.Current, e4.Current);
        }
    }
}
