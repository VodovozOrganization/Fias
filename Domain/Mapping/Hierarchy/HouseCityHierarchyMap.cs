using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class HouseCityHierarchyMap : ClassMap<HouseCityHierarchy>
	{
		public HouseCityHierarchyMap()
		{
			Table("house_city_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasHouseGuid, "fias_house_guid")
				.KeyProperty(x => x.FiasCityGuid, "fias_city_guid");
		}
	}
}
