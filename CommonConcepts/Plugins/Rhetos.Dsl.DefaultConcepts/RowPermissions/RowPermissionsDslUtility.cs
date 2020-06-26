using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.DefaultConcepts
{
    public class RowPermissionsDslUtility
    {
		public static List<Tuple<string, DataStructureInfo>> GetInherited(IDslModel dslModel, DataStructureInfo dataStructure, string property)
		{
			var rowPermission = dslModel.FindByReference<RowPermissionsPluginableFiltersInfo>(x => x.DataStructure, dataStructure).FirstOrDefault();

			if (rowPermission == null && string.IsNullOrEmpty(property))
				return new List<Tuple<string, DataStructureInfo>> {};

			if (rowPermission == null)
				return new List<Tuple<string, DataStructureInfo>> { new Tuple<string, DataStructureInfo>(property, dataStructure) };

			var inheritedRowPermissions = dslModel.FindByReference<RowPermissionsInheritFromInfo>(x => x.RowPermissionsFilters, rowPermission);

			if (!inheritedRowPermissions.Any() && string.IsNullOrEmpty(property))
				return new List<Tuple<string, DataStructureInfo>> ();

			var results = new List<Tuple<string, DataStructureInfo>>();
			if(HasOwnRowPermission(dslModel, rowPermission) && !string.IsNullOrEmpty(property))
				results.Add(new Tuple<string, DataStructureInfo>(property, dataStructure));

			foreach (var inheritedRowPermission in inheritedRowPermissions)
				results.AddRange(GetInherited(dslModel, inheritedRowPermission.Source, string.IsNullOrEmpty(property) ? inheritedRowPermission.SourceSelector : property + "." + inheritedRowPermission.SourceSelector));

			return results;
		}

		private static bool HasOwnRowPermission(IDslModel dslModel, RowPermissionsPluginableFiltersInfo rowPermissionsFilters)
		{
			return dslModel.FindByReference<RowPermissionsRuleAllowReadInfo>(x => x.RowPermissionsFilters, rowPermissionsFilters).Any() ||
				dslModel.FindByReference<RowPermissionsRuleDenyReadInfo>(x => x.RowPermissionsFilters, rowPermissionsFilters).Any() ||
				dslModel.FindByReference<RowPermissionsRuleDenyInfo>(x => x.RowPermissionsFilters, rowPermissionsFilters).Any();
		}
	}
}
