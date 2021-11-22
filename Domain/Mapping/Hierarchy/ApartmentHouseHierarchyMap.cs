using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class ApartmentHouseHierarchyMap : ClassMap<ApartmentHouseHierarchy>
	{
		public ApartmentHouseHierarchyMap()
		{
			Table("apartment_house_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasApartmentGuid, "fias_apartment_guid")
				.KeyProperty(x => x.FiasHouseGuid, "fias_house_guid");
		}
	}
}
