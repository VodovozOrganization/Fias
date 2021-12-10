using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	public class HouseCoordinateMap : ClassMap<HouseCoordinate>
	{
		public HouseCoordinateMap()
		{
			Schema("public");
			Table("house_coordinate");
			Id(x => x.HouseFiasGuid).Column("house_fias_guid").GeneratedBy.Assigned();
			Map(x => x.Latitude).Column("lat");
			Map(x => x.Longitude).Column("lon");
		}
	}
}

