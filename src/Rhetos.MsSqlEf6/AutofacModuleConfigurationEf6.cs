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

using Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dom.DefaultConcepts.Persistence;
using Rhetos.Extensibility;
using Rhetos.MsSqlEf6.SqlResources;
using Rhetos.Persistence;
using Rhetos.SqlResources;
using Rhetos.Utilities;
using System.ComponentModel.Composition;
using System.Data.Common;

namespace Rhetos.MsSqlEf6
{
    [Export(typeof(Module))]
    public class AutofacModuleConfigurationEf6 : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // SqlClientFactory.Instance is null at this point (Autofac Module Load), is expected to get initialized later.
            builder.Register<DbProviderFactory>(context => SqlClientFactory.Instance).SingleInstance();

            ExecutionStage stage = builder.GetRhetosExecutionStage();

            if (stage.IsBuildTime)
            {
                builder.RegisterType<EntityFrameworkMappingGenerator>().As<IGenerator>();
                builder.RegisterType<MsSqlEf6SqlResourcesPlugin>().As<ISqlResourcesPlugin>().SingleInstance();
            }

            if (stage.IsApplicationInitialization)
            {
                builder.RegisterDecorator<DbUpdateOptions>((context, parameters, originalOptions) =>
                {
                    // EfMappingViewsInitializer needs to be executed before other initializers, because of performance issues.
                    // EfMappingViewsInitializer is needed for most of the initializer to run, but lazy initialization of EfMappingViews on first DbContext usage
                    // is not a good option because of significant hash check duration: That lazy initialization would slow down the application at run-time,
                    // where it is actually not needed.
                    DbUpdateOptions modifiedOptions = CsUtility.ShallowCopy(originalOptions);
                    modifiedOptions.OverrideServerInitializerOrdering[typeof(EfMappingViewsInitializer).FullName] = -100;
                    return modifiedOptions;
                });
            }

            if (stage.IsRuntime)
            {
                builder.RegisterType<EfMappingViewsFileStore>().SingleInstance().PreserveExistingDefaults();
                builder.RegisterType<EfMappingViewCacheFactory>().SingleInstance().PreserveExistingDefaults();
                builder.RegisterType<EfMappingViewsInitializer>().SingleInstance();

                // CommonConcepts:
                builder.RegisterType<EntityFrameworkMetadata>().SingleInstance();
                builder.RegisterType<MetadataWorkspaceFileProvider>().As<IMetadataWorkspaceFileProvider>().SingleInstance();
                builder.RegisterType<EfMappingViewsHash>().As<IEfMappingViewsHash>();
                builder.RegisterType<Ef6OrmUtility>().As<IOrmUtility>().SingleInstance();
            }

            base.Load(builder);
        }
    }
}