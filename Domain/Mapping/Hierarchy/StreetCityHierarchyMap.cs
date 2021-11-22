using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class StreetCityHierarchyMap : ClassMap<StreetCityHierarchy>
	{
		public StreetCityHierarchyMap()
		{
			Table("street_city_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasStreetGuid, "fias_street_guid")
				.KeyProperty(x => x.FiasCityGuid, "fias_city_guid");
		}
	}
}
