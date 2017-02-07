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
using System.Diagnostics.Contracts;
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using System.Xml.Serialization;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.XmlSerialization;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandInfo))]
    public class SaveEntityCommandInfo : ICommandInfo
    {
        public string Entity { get; set; }
        public IEntity[] DataToInsert { get; set; }
        public IEntity[] DataToUpdate { get; set; }
        public IEntity[] DataToDelete { get; set; }

        public override string ToString()
        {
            return GetType().Name
                + " " + Entity
                + (DataToInsert != null && DataToInsert.Length > 0 ? ", insert " + DataToInsert.Length : "")
                + (DataToUpdate != null && DataToUpdate.Length > 0 ? ", update " + DataToUpdate.Length : "")
                + (DataToDelete != null && DataToDelete.Length > 0 ? ", delete " + DataToDelete.Length : "");
        }
    }
}
