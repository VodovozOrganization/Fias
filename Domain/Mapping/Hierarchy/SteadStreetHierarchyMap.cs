using Fias.Domain.Entities.Hierarchy;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping.Hierarchy
{
	public class SteadStreetHierarchyMap : ClassMap<SteadStreetHierarchy>
	{
		public SteadStreetHierarchyMap()
		{
			Table("stead_street_hierarchy");
			CompositeId()
				.KeyProperty(x => x.FiasSteadGuid, "fias_stead_guid")
				.KeyProperty(x => x.FiasStreetGuid, "fias_street_guid");
		}
	}
}
