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
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Processing.DefaultCommands;
using System.Linq;
using Rhetos.Utilities;
using Rhetos.Processing;

namespace Rhetos.Security.Service
{
    public class RestImpl
    {
        private readonly IProcessingEngine _processingEngine;
        private readonly IDomainObjectModel _domainObjectModel;

        private const string _eClaim = "Common.Claim";
        private const string _ePermission = "Common.Permission";
        private const string _ePrincipal = "Common.Principal";

        public RestImpl(IProcessingEngine processingEngine, IDomainObjectModel domainObjectModel)
        {
            _processingEngine = processingEngine;
            _domainObjectModel = domainObjectModel;
        }

        private object[] ReadEntity(string dataSource, FilterCriteria[] genericFilter = null)
        {
            var processingResult = _processingEngine.Execute(new[] {
                new QueryDataSourceCommandInfo
                {
                    DataSource = dataSource,
                    GenericFilter = genericFilter
                }});

            ThrowExceptionOnError(processingResult);

            var queryResult = (Rhetos.XmlSerialization.XmlBasicData<QueryDataSourceCommandResult>)processingResult.CommandResults[0].Data;
            return queryResult.Data.Records;
        }

        private void SaveEntity(SaveEntityCommandInfo commandInfo)
        {
            var processingResult = _processingEngine.Execute(new[] { commandInfo });
            ThrowExceptionOnError(processingResult);
        }

        private void ThrowExceptionOnError(ProcessingResult processingResult)
        {
            if (!processingResult.Success)
                if (processingResult.UserMessage != null)
                    throw new UserException(processingResult.UserMessage, processingResult.SystemMessage);
                else
                    throw new FrameworkException(processingResult.SystemMessage);
        }

        public List<Principal> GetPrincipals()
        {
            var rawData = ReadEntity(_ePrincipal);

            List<Principal> items = new List<Principal>();

            foreach (dynamic data in rawData)
            {
                Principal item = new Principal();
                item.ID = data.ID;
                item.Name = data.Name;
                items.Add(item);
            }

            return items;
        }

        public void UpdatePrincipalName(Guid id, string name)
        {
            SaveEntityCommandInfo commandInfo = new SaveEntityCommandInfo();
            commandInfo.Entity = _ePrincipal;
            dynamic principal = CreateEntity(_ePrincipal);
            principal.ID = id;
            principal.Name = name;
            commandInfo.DataToUpdate = new IEntity[] { principal };

            SaveEntity(commandInfo);
        }

        public void CreatePrincipal(string name)
        {
            SaveEntityCommandInfo commandInfo = new SaveEntityCommandInfo();
            commandInfo.Entity = _ePrincipal;
            dynamic principal = CreateEntity(_ePrincipal);
            principal.ID = Guid.NewGuid();
            principal.Name = name;
            commandInfo.DataToInsert = new IEntity[] { principal };

            SaveEntity(commandInfo);
        }

        public void DeletePrincipal(Guid id)
        {
            SaveEntityCommandInfo commandInfo = new SaveEntityCommandInfo();
            commandInfo.Entity = _ePrincipal;
            dynamic principal = CreateEntity(_ePrincipal);
            principal.ID = id;
            commandInfo.DataToDelete = new IEntity[] { principal };

            SaveEntity(commandInfo);
        }

        public List<Claim> GetClaims()
        {
            var rawData = ReadEntity(_eClaim,
                new[] { new FilterCriteria { Property = "Active", Operation = "Equal", Value = true } });

            List<Claim> items = new List<Claim>();

            foreach (dynamic data in rawData)
            {
                Claim item = new Claim();
                item.ID = data.ID;
                item.ClaimRight = data.ClaimRight;
                item.ClaimResource = data.ClaimResource;
                items.Add(item);
            }

            items.Sort(new ClaimComparer());

            return items;
        }
        private class ClaimComparer : IComparer<Claim>
        {
            #region IComparer<Claim> Members

            public int Compare(Claim x, Claim y)
            {
                return x.ClaimResource.CompareTo(y.ClaimResource) * 10 + x.ClaimRight.CompareTo(y.ClaimRight);
            }

            #endregion
        }

        public List<Permission> GetPermissions(Guid principalID)
        {
            var rawData = ReadEntity(_ePermission,
                new[]
                {
                    new FilterCriteria { Property = "Principal.ID", Operation = "Equal", Value = principalID.ToString() },
                    new FilterCriteria { Property = "Claim.Active", Operation = "Equal", Value = true },
                });

            List<Permission> items = new List<Permission>();

            foreach (dynamic data in rawData)
            {
                Permission item = new Permission();
                item.ID = data.ID;
                item.ClaimID = data.ClaimID;
                item.PrincipalID = data.PrincipalID;
                item.IsAuthorized = data.IsAuthorized;
                items.Add(item);
            }

            return items;
        }

        public void ApplyPermissionChange(Guid principalID, Guid claimID, string isAuthorized)
        {
            bool? authorized = ConvertIsAuthorized(isAuthorized);

            if (authorized == null)
                DeletePermission(principalID, claimID);
            else
            {
                SaveEntityCommandInfo commandInfo = new SaveEntityCommandInfo();
                commandInfo.Entity = _ePermission;

                dynamic permission = FindPermission(principalID, claimID);
                if (permission == null)
                {
                    permission = CreateEntity(_ePermission);
                    permission.ID = Guid.NewGuid();
                    permission.PrincipalID = principalID;
                    permission.ClaimID = claimID;
                    permission.IsAuthorized = (bool) authorized;
                    commandInfo.DataToInsert = new IEntity[] { permission };
                }
                else
                {
                    permission.IsAuthorized = (bool) authorized;
                    commandInfo.DataToUpdate = new IEntity[] { permission };
                }

                SaveEntity(commandInfo);
            }
        }

        private void DeletePermission(Guid principalID, Guid claimID)
        {
            dynamic permission = FindPermission(principalID, claimID);
            if (permission == null) 
                return;

            SaveEntityCommandInfo commandInfo = new SaveEntityCommandInfo();
            commandInfo.Entity = _ePermission;
            commandInfo.DataToDelete = new IEntity[] { permission };
            SaveEntity(commandInfo);
        }

        private dynamic FindPermission(Guid principalID, Guid claimID)
        {
            return ReadEntity(_ePermission, new[]
                {
                        new FilterCriteria { Property = "Principal.ID", Operation = "Equal", Value = principalID.ToString() },
                        new FilterCriteria { Property = "Claim.ID", Operation = "Equal", Value = claimID.ToString() }
                }).FirstOrDefault();
        }

        private dynamic CreateEntity(string typeName)
        {
            var t = _domainObjectModel.GetType(typeName);
            return Activator.CreateInstance(t);
        }

        private static bool? ConvertIsAuthorized(string isAuthorized)
        {
            switch (isAuthorized.ToLower())
            {
                case "true":
                    return true;
                case "false":
                    return false;
                case "null":
                    return null;
                default:
                    throw new ArgumentOutOfRangeException("isAuthorized", "Allowed values are 'true', 'false' and 'null'.");
            }
        }
    }
}