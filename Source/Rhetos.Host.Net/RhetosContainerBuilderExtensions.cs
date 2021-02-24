using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Rhetos.Utilities;

namespace Rhetos.Extensions.NetCore
{
    public static class RhetosContainerBuilderExtensions
    {
        public static void UseUserInfoProvider(this ContainerBuilder containerBuilder, Func<IUserInfo> userInfoProvider)
        {
            containerBuilder.Register(_ => userInfoProvider()).As<IUserInfo>().InstancePerLifetimeScope();
        }
    }
}
