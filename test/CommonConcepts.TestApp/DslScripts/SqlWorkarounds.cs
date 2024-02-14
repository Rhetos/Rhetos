using Rhetos.Utilities;
using System.Collections.Generic;

namespace TestSqlWorkarounds.Repositories
{
    public partial class PersonInfo_Repository
    {
        public partial IEnumerable<PersonInfo> Load(PersonFilter parameter)
        {
            var result = new List<PersonInfo>();

            // Always use *interpolated* with parameters to prevent SQL injection.
            _executionContext.SqlExecuter.ExecuteReaderInterpolated(
                $"EXEC TestSqlWorkarounds.ComputePersonInfo {parameter.NamePattern}, {parameter.LimitResultCount}",
                read => result.Add(new PersonInfo
                {
                    ID = read.GetGuid(0),
                    Name = read.GetString(1),
                    NameLength = read.GetInt32(2),
                    PersonID = read.GetGuid(3),
                }));

            return result;
        }
    }
}
