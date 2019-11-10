using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Extensibility;
using Rhetos.TestCommon;
using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class LegacyUtilitiesTests
    {
        public LegacyUtilitiesTests()
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();

            LegacyUtilities.Initialize(configurationProvider);
        }

        [TestMethod]
        public void SqlUtilityWorksCorrectly()
        {
            Assert.AreEqual("MsSql", SqlUtility.DatabaseLanguage);
            Assert.IsFalse(string.IsNullOrEmpty(SqlUtility.ConnectionString));
            Assert.AreEqual(31, SqlUtility.SqlCommandTimeout);
        }

        [TestMethod]
        public void PathThrowsOnNullEnvironment()
        {
            // this will also set environment to null
            TestUtility.ShouldFail<ArgumentNullException>(() => Paths.Initialize(null));
            
            TestUtility.ShouldFail<FrameworkException>(() => Console.WriteLine(Paths.RhetosServerRootPath), "Rhetos server is not initialized (Paths class)");
            TestUtility.ShouldFail<FrameworkException>(() => Console.WriteLine(Paths.GeneratedFilesCacheFolder), "Rhetos server is not initialized (Paths class)");
        }
    }
}
