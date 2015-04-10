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
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Persistence.NHibernate;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
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

        public const string AdminUserName = "admin";
        public const string AdminRoleName = "SecurityAdministrator";

        public void Initialize()
        {
            CreateAdminUserAndPermissions(_repositories);
            InitializeAspNetDatabase();
        }

        public static void CreateAdminUserAndPermissions(GenericRepositories repositories)
        {
            var adminPrincipal = repositories.CreateInstance<IPrincipal>();
            adminPrincipal.Name = AdminUserName;
            repositories.InsertOrReadId(adminPrincipal, item => item.Name);

            var adminRole = repositories.CreateInstance<IRole>();
            adminRole.Name = AdminRoleName;
            repositories.InsertOrReadId(adminRole, item => item.Name);

            var adminPrincipalHasRole = repositories.CreateInstance<IPrincipalHasRole>();
            adminPrincipalHasRole.PrincipalID = adminPrincipal.ID;
            adminPrincipalHasRole.RoleID = adminRole.ID;
            repositories.InsertOrReadId(adminPrincipalHasRole, item => new { PrincipalID = item.Principal.ID, RoleID = item.Role.ID });

            foreach (var securityClaim in AuthenticationServiceClaims.GetDefaultAdminClaims())
            {
                var commonClaim = repositories.CreateInstance<ICommonClaim>();
                commonClaim.ClaimResource = securityClaim.Resource;
                commonClaim.ClaimRight = securityClaim.Right;
                repositories.InsertOrReadId(commonClaim, item => new { item.ClaimResource, item.ClaimRight });

                var permission = repositories.CreateInstance<IRolePermission>();
                permission.RoleID = adminRole.ID;
                permission.ClaimID = commonClaim.ID;
                permission.IsAuthorized = true;
                repositories.InsertOrUpdateReadId(permission, item => new { RoleID = item.Role.ID, ClaimID = item.Claim.ID }, item => item.IsAuthorized);
            }
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
            // Committing the inserted user data so it can be used by InitAspNetDatabase.exe.
            _persistenceTransaction.CommitAndReconnect();

            var path = Path.Combine(Paths.PluginsFolder, @"InitAspNetDatabase.exe");
            ExecuteApplication(path, "/nopause");
        }

        private void ExecuteApplication(string path, string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo(path)
            {
                Arguments = arguments,
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
}
