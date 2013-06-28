/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Compiler;
using System.Globalization;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(CreationTimeInfo))]
    public class CreationTimeDatabaseDefinition : IConceptDatabaseDefinition
    {
        private SqlTriggerInfo GeneratedTrigger(CreationTimeInfo info)
        {
            return new SqlTriggerInfo
            {
                Structure = info.Property.DataStructure,
                Name = "CreationTime",
                Events = "FOR INSERT",
                TriggerSource = Sql.Format("CreationTimeDatabaseDefinition_TriggerBody",
                    SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.Property.DataStructure.Name),
                    SqlUtility.Identifier(info.Property.Name))
            };
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (CreationTimeInfo)conceptInfo;
            var trigger = GeneratedTrigger(info);
            return new SqlTriggerDatabaseDefinition().CreateDatabaseStructure(trigger);
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (CreationTimeInfo)conceptInfo;
            var trigger = GeneratedTrigger(info);
            return new SqlTriggerDatabaseDefinition().RemoveDatabaseStructure(trigger);
        }
    }
}
