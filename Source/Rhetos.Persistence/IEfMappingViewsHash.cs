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
using System.Text;

namespace Rhetos.Persistence
{
    /// <summary>
    /// According to on-line documentation, we should use the following code (with no need for this interface).
    /// <code>
    /// var mappingCollection = (StorageMappingItemCollection)_metadataWorkspaceFileProvider.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
    /// return mappingCollection.ComputeMappingHashValue();
    /// </code>
    /// There is an issue with that hash: is does not detect changes in C# property ordering,
    /// which results with an error in EF when compiling LINQ query to SQL query.
    /// For example, switching positions of properties IsAuthorized and RoleID in Common.RolePermission results with exception at <c>repository.Common.PrincipalHasRole.Query().ToString()</c>:
    /// System.Data.Entity.Core.EntityCommandCompilationException: An error occurred while preparing the command definition. See the inner exception for details. ---> System.Data.Entity.Core.MappingException: The query view generated for the EntitySet 'Common_RolePermission' is not valid. The query parser threw the following error : The argument type 'Edm.Guid' is not compatible with the property 'IsAuthorized' of formal type 'Edm.Boolean'. Near member access expression, line 3, column 109..
    /// </summary>
    public interface IEfMappingViewsHash
    {
        string GetAdditionalHash();
    }
}
