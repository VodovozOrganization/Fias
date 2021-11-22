using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class HouseStreetHierarchyMap : ClassMap<HouseStreetHierarchy>
	{
		public HouseStreetHierarchyMap()
		{
			Table("house_street_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasHouseGuid, "fias_house_guid")
				.KeyProperty(x => x.FiasStreetGuid, "fias_street_guid");
		}
	}
}
