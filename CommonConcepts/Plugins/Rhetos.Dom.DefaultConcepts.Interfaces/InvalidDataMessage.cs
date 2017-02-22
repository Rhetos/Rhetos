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

using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dom.DefaultConcepts
{
    public class InvalidDataMessage
    {
        public string Message;
        public object[] MessageParameters = _emptyArray;
        /// <summary>Optional.</summary>
        public Guid? ID;
        /// <summary>Optional.</summary>
        public string Property;

        private static readonly object[] _emptyArray = new object[] { };

        public static void ValidateOnSave(IEnumerable<IEntity> inserted, IEnumerable<IEntity> updated, IValidateRepository repository, string dataStructure)
        {
            if (inserted.Count() > 0 || updated.Count() > 0)
            {
                Guid[] newItemsIds = inserted.Concat(updated).Select(item => item.ID).ToArray();
                var error = repository.Validate(newItemsIds, onSave: true).FirstOrDefault();
                if (error != null)
                {
                    string systemMessage = "DataStructure:" + dataStructure
                        + (error.ID != null ? ",ID:" + error.ID.Value.ToString() : "")
                        + (error.Property != null ? ",Property:" + error.Property : "");

                    throw new UserException(error.Message, error.MessageParameters, systemMessage, null);
                }
            }
        }
    }
}
