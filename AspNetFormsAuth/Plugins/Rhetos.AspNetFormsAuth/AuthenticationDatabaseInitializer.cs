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

using Autofac.Features.Indexed;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Processing;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.Text;

namespace Rhetos.AspNetFormsAuth
{
    // Executes at deployment-time.
    [Export(typeof(Rhetos.Extensibility.IServerInitializer))]
    public class AuthenticationDatabaseInitializer : Rhetos.Extensibility.IServerInitializer
    {
        public void Initialize()
        {
            var adminPrincipal = CreateEntity<IPrincipal>();
            adminPrincipal.Name = adminUserName;
            InsertOrReadId(adminPrincipal, item => item.Name == adminPrincipal.Name, item => item.Name);

            var adminRole = CreateEntity<IRole>();
            adminRole.Name = adminRoleName;
            InsertOrReadId(adminRole, item => item.Name == adminRole.Name, item => item.Name);

            var adminPrincipalHasRole = CreateEntity<IPrincipalHasRole>();
            adminPrincipalHasRole.PrincipalID = adminPrincipal.ID;
            adminPrincipalHasRole.RoleID = adminRole.ID;
            InsertOrReadId(adminPrincipalHasRole,
                item => item.Principal.ID == adminPrincipal.ID && item.Role.ID == adminRole.ID, item => item.PrincipalID.ToString() + "/" + item.RoleID.ToString());

            foreach (var securityClaim in AuthenticationServiceClaims.GetAdminClaims())
            {
                var commonClaim = CreateEntity<ICommonClaim>();
                commonClaim.ClaimResource = securityClaim.Resource;
                commonClaim.ClaimRight = securityClaim.Right;
                InsertOrReadId(commonClaim,
                    item => item.ClaimResource == commonClaim.ClaimResource && item.ClaimRight == commonClaim.ClaimRight,
                    item => item.ClaimResource + "." + item.ClaimRight);

                var permission = CreateEntity<IPermission>();
                permission.RoleID = adminRole.ID;
                permission.ClaimID = commonClaim.ID;
                permission.IsAuthorized = true;
                InsertOrUpdateReadId(permission, item => item.Role.ID == adminRole.ID && item.Claim.ID == commonClaim.ID,
                    item => adminRole.Name + " " + commonClaim.ClaimResource + "." + commonClaim.ClaimRight);
            }

            InitializeAspNetDatabase();
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }

        private readonly IIndex<string, IWritableRepository> _writableRepositories;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly ILogger _logger;

        public AuthenticationDatabaseInitializer(
            IIndex<string, IWritableRepository> writableRepositories,
            IDomainObjectModel domainObjectModel,
            ILogProvider logProvider)
        {
            _writableRepositories = writableRepositories;
            _domainObjectModel = domainObjectModel;
            _logger = logProvider.GetLogger("AuthenticationDatabaseInitializer");
        }

        const string adminUserName = "admin";
        const string adminRoleName = "SecurityAdministrator";

        private static Dictionary<Type, string> EntityNameByInterface = new Dictionary<Type, string>
        {
            { typeof(IRole), "Common.Role" },
            { typeof(IPrincipal), "Common.Principal" },
            { typeof(IPrincipalHasRole), "Common.PrincipalHasRole" },
            { typeof(ICommonClaim), "Common.Claim" },
            { typeof(IPermission), "Common.Permission" },
        };

        private static string GetEntityName<TEntityInterface>()
        {
            string entityName;
            if (!EntityNameByInterface.TryGetValue(typeof(TEntityInterface), out entityName))
                throw new FrameworkException("Undefined type " + typeof(TEntityInterface).FullName + ".");
            return entityName;
        }

        private TEntityInterface CreateEntity<TEntityInterface>()
        {
            Type entityType = _domainObjectModel.GetType(GetEntityName<TEntityInterface>());
            return (TEntityInterface)Activator.CreateInstance(entityType);
        }

        private void InsertOrReadId<TEntityInterface>(
            TEntityInterface item,
            Expression<Func<TEntityInterface, bool>> itemFilter,
            Func<TEntityInterface, string> itemDescription)
            where TEntityInterface : IEntity
        {
            string entityName = GetEntityName<TEntityInterface>();
            var writableRepos = _writableRepositories[entityName];
            var queryableRepos = (IQueryableRepository<TEntityInterface>)writableRepos;

            Guid id = queryableRepos.Query().Where(itemFilter).Select(e => e.ID).SingleOrDefault();
            if (id == default(Guid))
            {
                _logger.Trace(() => "Creating " + entityName + " '" + itemDescription(item) + "'.");
                writableRepos.Save(new[] { (object)item }, null, null);
            }
            else
            {
                _logger.Trace(() => "Already exists " + entityName + " '" + itemDescription(item) + "'.");
                item.ID = id;
            }
        }

        private void InsertOrUpdateReadId<TEntityInterface>(
            TEntityInterface item,
            Expression<Func<TEntityInterface, bool>> itemFilter,
            Func<TEntityInterface, string> itemDescription)
            where TEntityInterface : IEntity
        {
            string entityName = GetEntityName<TEntityInterface>();
            var writableRepos = _writableRepositories[entityName];
            var queryableRepos = (IQueryableRepository<TEntityInterface>)writableRepos;

            TEntityInterface old = queryableRepos.Query().Where(itemFilter).ToList().SingleOrDefault();
            if (old == null)
            {
                _logger.Trace(() => "Creating " + entityName + " '" + itemDescription(item) + "'.");
                writableRepos.Save(new[] { (object)item }, null, null);
            }
            else
            {
                item.ID = old.ID;
                bool same = true;
                if (((IPermission)old).IsAuthorized != ((IPermission)item).IsAuthorized)
                {
                    ((IPermission)old).IsAuthorized = ((IPermission)item).IsAuthorized;
                    same = false;
                }
                if (!same)
                {
                    writableRepos.Save(null, new[] { (object)old }, null);
                    _logger.Trace(() => "Updating " + entityName + " '" + itemDescription(item) + "'.");
                }
                else
                    _logger.Trace(() => "Already exists " + entityName + " '" + itemDescription(item) + "'.");
            }
        }

        /// <summary>
        /// The initialization is placed in a separate application, because the SimpleMembershipProvider functions
        /// require some configuration data in app.config file. The changes in app.config cannot be added as a plugin for
        /// DeployPackages.exe.
        /// </summary>
        private void InitializeAspNetDatabase()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Plugins\InitAspNetDatabase.exe");
            ExecuteApplication(path);
        }

        private void ExecuteApplication(string path)
        {
            ProcessStartInfo start = new ProcessStartInfo(path)
            {
                Arguments = "/nopause",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            string processOutput;
            int processErrorCode;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    processOutput = reader.ReadToEnd();
                    processOutput = processOutput.Trim();
                }
                process.WaitForExit();
                processErrorCode = process.ExitCode;
            }

            _logger.Trace(() => Path.GetFileName(path) + " error code: " + processErrorCode);
            _logger.Trace(() => Path.GetFileName(path) + " output: " + processOutput);

            if (processErrorCode != 0)
                throw new FrameworkException(Path.GetFileName(path) + " returned an error: " + processOutput);
        }
    }

    [Export(typeof(IClaimProvider))]
    [ExportMetadata(MefProvider.Implements, typeof(DummyCommandInfo))]
    public class AuthenticationServiceClaims : IClaimProvider
    {
        public IList<Claim> GetRequiredClaims(ICommandInfo info)
        {
            return null;
        }

        public IList<Claim> GetAllClaims(Dsl.IDslModel dslModel)
        {
            return GetAdminClaims();
        }

        public static IList<Claim> GetAdminClaims()
        {
            return new[] { SetPasswordClaim, UnlockUserClaim, IgnorePasswordStrengthPolicyClaim, GeneratePasswordResetTokenClaim };
        }

        public static readonly Claim SetPasswordClaim = new Claim("AspNetFormsAuth.AuthenticationService", "SetPassword");
        public static readonly Claim UnlockUserClaim = new Claim("AspNetFormsAuth.AuthenticationService", "UnlockUser");
        public static readonly Claim IgnorePasswordStrengthPolicyClaim = new Claim("AspNetFormsAuth.AuthenticationService", "IgnorePasswordStrengthPolicy");
        public static readonly Claim GeneratePasswordResetTokenClaim = new Claim("AspNetFormsAuth.AuthenticationService", "GeneratePasswordResetToken");
    }

    public class DummyCommandInfo : ICommandInfo
    {
    }
}
