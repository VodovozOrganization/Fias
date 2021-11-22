using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class OtherHierarchyMap : ClassMap<OtherHierarchy>
	{
		public OtherHierarchyMap()
		{
			Table("other_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasGuid, "fias_guid")
				.KeyProperty(x => x.FiasParentGuid, "fias_parent_guid");
		}
	}
}
