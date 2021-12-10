using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class StreetMunDistrictHierarchyMap : ClassMap<StreetMunDistrictHierarchy>
	{
		public StreetMunDistrictHierarchyMap()
		{
			Schema("public");
			Table("street_mun_district_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasStreetGuid, "fias_street_guid")
				.KeyProperty(x => x.FiasMunDistrictGuid, "fias_mun_district_guid");
		}
	}
}
