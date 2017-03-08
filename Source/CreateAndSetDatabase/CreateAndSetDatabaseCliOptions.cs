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
using System.Threading.Tasks;

namespace CreateAndSetDatabase
{
    public class CreateAndSetDatabaseCliOptions : CliOptions
    {
        public bool Help { get; set; }

        public string ServerName { get; set; }

        public bool UseSSPI { get; set; }

        public string UserId { get; set; }

        public string Password { get; set; }

        public string DatabaseName { get; set; }

        public string ProviderName { get; set; }

        public CreateAndSetDatabaseCliOptions()
            : base()
        {
            this.ProviderName = "Rhetos.MsSql";
        }

        public override void Initialize()
        {
            Help = this.Options.ContainsKey("-h");

            if (Help)
            {
                return;
            }

            if (this.Arguments.Length < 2)
            {
                throw new ArgumentException("SQLServer must be specified.");
            }

            ServerName = this.Arguments[0];
            DatabaseName = this.Arguments[1];

            if (this.Options.ContainsKey("-provider"))
            {
                ProviderName = this.Options["-provider"];
            }

            UseSSPI = !this.Options.ContainsKey("-c");
            if (!UseSSPI)
            {
                if (!this.Options.ContainsKey("-userid"))
                {
                    Console.Write("Database user name: ");
                    UserId = Console.ReadLine();
                }
                else
                {
                    UserId = this.Options["-userid"];
                }

                if (!this.Options.ContainsKey("-pw"))
                {
                    Console.Write("Database password: ");
                    Password = Console.ReadLine();
                }
                else
                {
                    Password = this.Options["-pw"];
                }
            }
        }

        public override bool RequireOptionArgument(string option)
        {
            var argumentRequiredOptions = new[] { "-userid", "-pw", "-provider" };

            return argumentRequiredOptions.Contains(option);
        }

        public static void ShowHelp()
        {
            Console.WriteLine(
@"Usage: CreateAndSetDatabase [options] <SQLServer> <DataBaseName>
    options:
        -h                  show help
        -c                  use datbase login credential instead of integrated security, if specified, userId and password will be asked
        -userid <UserId>    database's user ID
        -pw <Password>      database's password
        -provider <Name>    (optional) database provider name
"
            );
        }
    }
}
