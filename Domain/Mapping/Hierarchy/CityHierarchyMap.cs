using Fias.Domain.Entities;
using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class CityHierarchyMap : ClassMap<CityHierarchy>
	{
		public CityHierarchyMap()
		{
			Table("city_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasCityGuid, "fias_city_guid")
				.KeyProperty(x => x.FiasParentObjectGuid, "fias_parent_object_guid");
		}
	}
}
