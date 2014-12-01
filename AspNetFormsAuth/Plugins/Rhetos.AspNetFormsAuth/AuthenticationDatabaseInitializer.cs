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
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Persistence.NHibernate;
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
        private readonly GenericRepositories _repositories;
        private readonly ILogger _logger;
        private readonly IPersistenceTransaction _persistenceTransaction;

        public AuthenticationDatabaseInitializer(
            GenericRepositories repositories,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction)
        {
            _repositories = repositories;
            _logger = logProvider.GetLogger("AuthenticationDatabaseInitializer");
            _persistenceTransaction = persistenceTransaction;
        }

        const string adminUserName = "admin";
        const string adminRoleName = "SecurityAdministrator";
        public void Initialize()
        {
            var adminPrincipal = _repositories.CreateInstance<IPrincipal>();
            adminPrincipal.Name = adminUserName;
            _repositories.InsertOrReadId(adminPrincipal, item => item.Name);

            var adminRole = _repositories.CreateInstance<IRole>();
            adminRole.Name = adminRoleName;
            _repositories.InsertOrReadId(adminRole, item => item.Name);

            var adminPrincipalHasRole = _repositories.CreateInstance<IPrincipalHasRole>();
            adminPrincipalHasRole.PrincipalID = adminPrincipal.ID;
            adminPrincipalHasRole.RoleID = adminRole.ID;
            _repositories.InsertOrReadId(adminPrincipalHasRole, item => new { PrincipalID = item.Principal.ID, RoleID = item.Role.ID });

            foreach (var securityClaim in AuthenticationServiceClaims.GetAdminClaims())
            {
                var commonClaim = _repositories.CreateInstance<ICommonClaim>();
                commonClaim.ClaimResource = securityClaim.Resource;
                commonClaim.ClaimRight = securityClaim.Right;
                _repositories.InsertOrReadId(commonClaim, item => new { item.ClaimResource, item.ClaimRight });

                var permission = _repositories.CreateInstance<IPermission>();
                permission.RoleID = adminRole.ID;
                permission.ClaimID = commonClaim.ID;
                permission.IsAuthorized = true;
                _repositories.InsertOrUpdateReadId(permission, item => new { RoleID = item.Role.ID, ClaimID = item.Claim.ID }, item => item.IsAuthorized);
            }

            ((NHibernatePersistenceTransaction)_persistenceTransaction).CommitAndReconnect();
            InitializeAspNetDatabase();
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }

        /// <summary>
        /// The initialization is placed in a separate application, because the SimpleMembershipProvider functions
        /// require some configuration data in app.config file. The changes in app.config cannot be added as a plugin for
        /// DeployPackages.exe.
        /// </summary>
        private void InitializeAspNetDatabase()
        {
            var path = Path.Combine(Paths.PluginsFolder, @"InitAspNetDatabase.exe");
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

            var processOutput = new StringBuilder();
            int processErrorCode;
            using (Process process = Process.Start(start))
            {
                var outputs = new[] { process.StandardOutput, process.StandardError };
                System.Threading.Tasks.Parallel.ForEach(outputs, output =>
                    {
                        using (StreamReader reader = output)
                        {
                            string line;
                            while ((line = output.ReadLine()) != null)
                                lock (processOutput)
                                    processOutput.AppendLine(line.Trim());
                        }
                    });
                
                process.WaitForExit();
                processErrorCode = process.ExitCode;
            }

            EventType logType = processErrorCode != 0 ? EventType.Error : EventType.Trace;
            _logger.Write(logType, () => Path.GetFileName(path) + " error code: " + processErrorCode);
            _logger.Write(logType, () => Path.GetFileName(path) + " output: " + processOutput.ToString());

            if (processErrorCode != 0)
                throw new FrameworkException(Path.GetFileName(path) + " returned an error: " + processOutput.ToString());
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

        public IList<Claim> GetAllClaims(IDslModel dslModel)
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
