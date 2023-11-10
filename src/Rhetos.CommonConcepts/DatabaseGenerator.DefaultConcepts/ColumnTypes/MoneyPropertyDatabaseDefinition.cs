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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using System;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseGenerator))]
    public class MoneyPropertyDatabaseDefinition : IConceptDatabaseGenerator<MoneyPropertyInfo>
    {
        private readonly ConceptMetadata _conceptMetadata;

        public MoneyPropertyDatabaseDefinition(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(MoneyPropertyInfo info, ISqlCodeBuilder sql)
        {
            string constraintName = sql.Utility.Identifier(sql.Resources.Format("MoneyPropertyDatabaseDefinition_CheckConstraintName",
                info.DataStructure.Name,
                info.Name));

            if (info.DataStructure is EntityInfo)
            {
                sql.Utility.Identifier(info.Name);

                sql.CreateDatabaseStructure(PropertyDatabaseDefinition.AddColumn(sql.Utility, sql.Resources, _conceptMetadata, info,
                    sql.Resources.Format("MoneyPropertyDatabaseDefinition_CreateCheckConstraint", constraintName, sql.Utility.Identifier(info.Name))));

                sql.RemoveDatabaseStructure(sql.Resources.Format("MoneyPropertyDatabaseDefinition_RemoveCheckConstraint",
                        sql.Utility.Identifier(info.DataStructure.Module.Name),
                        sql.Utility.Identifier(info.DataStructure.Name),
                        constraintName)
                    + Environment.NewLine
                    + PropertyDatabaseDefinition.RemoveColumn(sql.Utility, sql.Resources, _conceptMetadata, info));
            }
        }
    }
}
