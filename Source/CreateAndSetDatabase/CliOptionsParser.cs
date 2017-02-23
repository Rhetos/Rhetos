using System;
using System.Collections.Generic;

namespace CreateAndSetDatabase
{
    public static class CliOptionsParser
    {
        public static T Parse<T>(string[] arguments, bool hasCommand = false)
            where T : CliOptions
        {
            var result = Activator.CreateInstance<T>();
            var argumentQueue = new Queue<string>(arguments);

            if (hasCommand)
            {
                result.Command = argumentQueue.Dequeue();
            }

            while(argumentQueue.Count > 0)
            {
                if (!argumentQueue.Peek().StartsWith("-"))
                {
                    break;
                }

                var option = argumentQueue.Dequeue();
                var optionArgument = string.Empty;

                if (result.RequireOptionArgument(option) && argumentQueue.Count > 0)
                {
                    if (!argumentQueue.Peek().StartsWith("-"))
                    {
                        optionArgument = argumentQueue.Dequeue();
                    }
                }

                result.Options.Add(option.ToLower(), optionArgument);
            }

            result.Arguments = argumentQueue.ToArray();

            result.Initialize();
            return result;
        }
    }

    public abstract class CliOptions
    {
        public CliOptions()
        {
            Options = new Dictionary<string, string>();
        }

        public string Command { get; set; }

        public Dictionary<string, string> Options { get; set; }

        public string[] Arguments { get; set; }

        public virtual void Initialize() { }

        public virtual bool RequireOptionArgument(string option)
        {
            return false;
        }
    }
}
