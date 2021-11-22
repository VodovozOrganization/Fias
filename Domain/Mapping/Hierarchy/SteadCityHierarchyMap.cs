using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class SteadCityHierarchyMap : ClassMap<SteadCityHierarchy>
	{
		public SteadCityHierarchyMap()
		{
			Table("stead_city_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasSteadGuid, "fias_stead_guid")
				.KeyProperty(x => x.FiasCityGuid, "fias_city_guid");
		}
	}
}
