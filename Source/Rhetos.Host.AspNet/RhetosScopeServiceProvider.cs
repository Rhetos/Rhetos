using System;
using Autofac;
using Rhetos.Persistence;
using Rhetos.Utilities;

namespace Rhetos.Extensions.AspNetCore
{
    internal sealed class RhetosScopeServiceProvider : IDisposable
    {
        private readonly UnitOfWorkScope unitOfWorkScope;

        public RhetosScopeServiceProvider(RhetosHost rhetosHost, IUserInfo rhetosUser)
        {
            unitOfWorkScope = rhetosHost.CreateScope(builder => builder.RegisterInstance(rhetosUser));
        }

        public T Resolve<T>()
        {
            return unitOfWorkScope.Resolve<T>();
        }

        public void Dispose()
        {
            unitOfWorkScope.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
