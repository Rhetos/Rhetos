using System;
using System.Collections.Generic;

namespace Rhetos.Dom.DefaultConcepts
{
	public interface IPersistanceStorageObjectMapper
	{
		Dictionary<string, System.Data.Common.DbParameter> GetParameters(IEntity entity);

		List<Guid> GetDependencies(IEntity entity);

		string GetTableName();
	}
}
