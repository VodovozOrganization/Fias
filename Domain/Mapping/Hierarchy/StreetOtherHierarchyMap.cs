using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class StreetOtherHierarchyMap : ClassMap<StreetOtherHierarchy>
	{
		public StreetOtherHierarchyMap()
		{
			Table("street_other_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasStreetGuid, "fias_street_guid")
				.KeyProperty(x => x.FiasParentGuid, "fias_street_parent_guid");
		}
	}
}
