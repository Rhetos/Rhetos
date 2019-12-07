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

namespace Rhetos.DatabaseGenerator.Test
{
    public class MockConceptApplicationRepository : IConceptApplicationRepository
    {
        public List<ConceptApplication> ConceptApplications { get; set; }
        public List<ConceptApplication> DeletedLog { get; private set; } = new List<ConceptApplication>();
        public List<ConceptApplication> InsertedLog { get; private set; } = new List<ConceptApplication>();
        public List<Tuple<ConceptApplication, ConceptApplication>> UpdatedLog { get; private set; } = new List<Tuple<ConceptApplication, ConceptApplication>>();

        public List<ConceptApplication> Load()
        {
            return ConceptApplications;
        }

        public List<string> DeleteMetadataSql(ConceptApplication oldCA)
        {
            DeletedLog.Add(oldCA);
            return new List<string> { $"del {oldCA.ConceptInfoKey}" };
        }

        public List<string> InsertMetadataSql(ConceptApplication newCA)
        {
            InsertedLog.Add(newCA);
            return new List<string> { $"ins {newCA.ConceptInfoKey}" };
        }

        private ConceptApplicationRepository _conceptApplicationRepository = new ConceptApplicationRepository(null);

        public List<string> UpdateMetadataSql(ConceptApplication newCA, ConceptApplication oldCA)
        {
            // This is called event if the metadata has not changed.
            // It is responsible for checking if the new CA has same metadata at the old one.
            var sql = _conceptApplicationRepository.UpdateMetadataSql(newCA, oldCA);
            Console.WriteLine($"[UpdateMetadataSql] {newCA.ConceptInfoKey}:{string.Concat(sql.Select(script => $"\r\n - {script}"))}.");

            if (sql.Any())
            {
                UpdatedLog.Add(Tuple.Create(newCA, oldCA));
                return new List<string> { $"upd {newCA.ConceptInfoKey}" };
            }
            else
                return new List<string> { };
        }
    }
}
