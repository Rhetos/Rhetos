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
            var argumentRequiredOptions = new[] { "-userid", "-pw" };

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
"
            );
        }
    }
}
