using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class StreetToStreetHierarchyMap : ClassMap<StreetToStreetHierarchy>
	{
		public StreetToStreetHierarchyMap()
		{
			Schema("public");
			Table("street_to_street_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasStreetGuid, "fias_street_guid")
				.KeyProperty(x => x.FiasParentStreetGuid, "fias_parent_street_guid");
		}
	}
}

