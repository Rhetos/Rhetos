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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <remarks>
    /// This class applies 'dbo' authorization to database schema by creating a separate ALTER AUTHORIZATION query,
    /// instead of adding the AUTHORIZATION option to the CREATE SCHEMA query, in order to avoid dropping
    /// and recreating all database objects on deployment when upgrading existing applications.
    /// The upgrade could be handled efficiently by a data-migration script that alters the scheme authorization
    /// and Rhetos.AppliedConcepts, but it wouldn't support optimized downgrade to a previous version of CommonConcepts package.
    /// </remarks>
    [Export(typeof(IConceptMacro))]
    public class ModuleAuthorizationMacro : IConceptMacro<ModuleInfo>
    {
        private readonly ISqlResources _sqlResources;

        public ModuleAuthorizationMacro(ISqlResources sqlResources)
        {
            _sqlResources = sqlResources;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(ModuleInfo conceptInfo, IDslModel existingConcepts)
        {
            return new[]
            {
                new SqlObjectInfo
                {
                    Module = conceptInfo,
                    Name = "SchemaAuthorization",
                    CreateSql = _sqlResources.Format("ModuleAuthorization_Create", conceptInfo.Name),
                    RemoveSql = ""
                }
            };
        }
    }
}
