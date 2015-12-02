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

using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptMacro))]
    public class ClearPrincipalCacheMacro : IConceptMacro<InitializationConcept>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(InitializationConcept conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            InjectClearCacheOnSave(newConcepts, "Principal",
                "Name",
                @"var principalInfos = ((IEnumerable<Rhetos.Dom.DefaultConcepts.IPrincipal>)insertedNew).Concat(updatedNew)
                    .Concat(updatedOld.Select(p => new Rhetos.Dom.DefaultConcepts.PrincipalInfo { ID = p.ID, Name = p.Name }))
                    .Concat(deletedOld.Select(p => new Rhetos.Dom.DefaultConcepts.PrincipalInfo { ID = p.ID, Name = p.Name }));
                    _authorizationDataCache.ClearCachePrincipals(principalInfos);");

            InjectClearCacheOnSave(newConcepts, "PrincipalHasRole",
                "PrincipalID",
                @"var principalIds = insertedNew.Concat(updatedNew).Select(item => item.PrincipalID)
                        .Concat(updatedOld.Select(item => item.PrincipalID))
                        .Concat(deletedOld.Select(item => item.PrincipalID))
                        .Where(pid => pid != null).Select(pid => pid.Value)
                        .Distinct().ToList();
                    var principalInfos = _domRepository.Common.Principal.Query(principalIds)
                        .Select(p => new Rhetos.Dom.DefaultConcepts.PrincipalInfo { ID = p.ID, Name = p.Name });
                    _authorizationDataCache.ClearCachePrincipals(principalInfos);");

            InjectClearCacheOnSave(newConcepts, "PrincipalPermission",
                "PrincipalID",
                @"var principalIds = insertedNew.Concat(updatedNew).Select(item => item.PrincipalID)
                        .Concat(updatedOld.Select(item => item.PrincipalID))
                        .Concat(deletedOld.Select(item => item.PrincipalID))
                        .Where(pid => pid != null).Select(pid => pid.Value)
                        .Distinct().ToList();
                    var principalInfos = _domRepository.Common.Principal.Query(principalIds)
                        .Select(p => new Rhetos.Dom.DefaultConcepts.PrincipalInfo { ID = p.ID, Name = p.Name });
                    _authorizationDataCache.ClearCachePrincipals(principalInfos);");

            InjectClearCacheOnSave(newConcepts, "Role",
                null,
                @"var roleIds = insertedNew.Concat(updatedNew).Concat(deletedIds).Select(r => r.ID);
                    _authorizationDataCache.ClearCacheRoles(roleIds);");

            InjectClearCacheOnSave(newConcepts, "RoleInheritsRole",
                "UsersFromID",
                @"var roleIds = insertedNew.Concat(updatedNew).Select(item => item.UsersFromID)
                        .Concat(updatedOld.Select(item => item.UsersFromID))
                        .Concat(deletedOld.Select(item => item.UsersFromID))
                        .Where(rid => rid != null).Select(rid => rid.Value);
                    _authorizationDataCache.ClearCacheRoles(roleIds);");

            InjectClearCacheOnSave(newConcepts, "RolePermission",
                "RoleID",
                @"var roleIds = insertedNew.Concat(updatedNew).Select(item => item.RoleID)
                        .Concat(updatedOld.Select(item => item.RoleID))
                        .Concat(deletedOld.Select(item => item.RoleID))
                        .Where(rid => rid != null).Select(rid => rid.Value);
                    _authorizationDataCache.ClearCacheRoles(roleIds);");

            return newConcepts;
        }

        private void InjectClearCacheOnSave(List<IConceptInfo> newConcepts, string entityName, string loadOldPropertyValue, string snippetClearCache)
        {
            var commonModule = new ModuleInfo { Name = "Common" };
            var entity = new EntityInfo { Module = commonModule, Name = entityName };
            var usesAuthorizationDataCache = new RepositoryUsesInfo
            {
                DataStructure = entity,
                PropertyType = "Rhetos.Dom.DefaultConcepts.AuthorizationDataCache, Rhetos.Dom.DefaultConcepts",
                PropertyName = "_authorizationDataCache"
            };
            var saveMethod = new SaveMethodInfo { Entity = entity };
            newConcepts.AddRange(new IConceptInfo[] { usesAuthorizationDataCache, saveMethod });

            if (loadOldPropertyValue != null)
            {
                var loadOldItems = new LoadOldItemsInfo { SaveMethod = saveMethod };
                var loadOldItemsProperty = new LoadOldItemsTakeInfo { LoadOldItems = loadOldItems, Path = loadOldPropertyValue };
                newConcepts.AddRange(new IConceptInfo[] { loadOldItems, loadOldItemsProperty });
            }

            var clearCache = new OnSaveUpdateInfo
            {
                SaveMethod = saveMethod,
                RuleName = "ClearAuthenticationCache",
                CsCodeSnippet = snippetClearCache
            };
            newConcepts.AddRange(new IConceptInfo[] { clearCache });
        }
    }
}
