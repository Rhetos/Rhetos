using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Extensibility
{
    public static class NamedPluginsExtensions
    {
        public static TPlugin GetPlugin<TPlugin>(this INamedPlugins<TPlugin> namedPlugins, string name)
        {
            var plugins = namedPlugins.GetPlugins(name);

            if (plugins.Count() == 0)
                throw new Rhetos.FrameworkException("There is no " + typeof(TPlugin).Name + " plugin named '" + name + "'.");

            if (plugins.Count() > 1)
                throw new Rhetos.FrameworkException("There is more than one " + typeof(TPlugin).Name + " plugin named '" + name
                    + "': " + plugins.First().GetType().FullName + ", " + plugins.Last().GetType().FullName + ".");

            return plugins.First();
        }
    }
}
