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
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;

namespace TestReading.Repositories
{
    public partial class Simple_Repository
    {
        public Simple[] CustomFilterA()
        {
            int claimsCount = _domRepository.Common.Claim.Query().Count();
            string userName = _executionContext.UserInfo.UserName;

            return new[]
            {
                new Simple { Name = "A1", Data = claimsCount.ToString() },
                new Simple { Name = "A2", Data = userName }
            };
        }

        public IQueryable<Common.Queryable.TestReading_Simple> CustomFilterB(
            IQueryable<Common.Queryable.TestReading_Simple> query)
        {
            return query.Where(item => item.Name.StartsWith("B"));
        }

        public Simple[] Load(string[] names)
        {
            return names.Select(name => new Simple { Name = name }).ToArray();
        }

        public IQueryable<Common.Queryable.TestReading_Simple> Filter(
            IQueryable<Common.Queryable.TestReading_Simple> query,
            Prefix prefix)
        {
            return query.Where(item => item.Name.StartsWith(prefix.Pattern));
        }
    }
}
