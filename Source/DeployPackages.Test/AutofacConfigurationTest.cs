using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployPackages.Test
{
    [TestClass]
    public class AutofacConfigurationTest
    {
        [TestMethod]
        public void ContainerContainsAllComponents()
        {
            Paths.InitializeRhetosServer();

            var builder = new ContainerBuilder();
            var arguments = new DeployArguments(new string[] { } ); // "/DatabaseOnly"
            builder.RegisterModule(new AutofacModuleConfiguration(deploymentTime: true, arguments));

            using (var container = builder.Build())
            {
                var regs = container.ComponentRegistry.Registrations
                    .Select(a => a.ToString())
                    .OrderBy(a => a)
                    .ToList();
                foreach (var r in regs)
                {
                    System.Diagnostics.Trace.WriteLine(r);
                }
            }
        }
    }
}
