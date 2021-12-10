using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	public class WrongOsmAddressMap : ClassMap<WrongOsmAddress>
	{
		public WrongOsmAddressMap()
		{
			Schema("public");
			Table("wrong_osm_addresses");
			Id(x => x.Id).Column("id").GeneratedBy.TriggerIdentity();
			Map(x => x.CityName).Column("city_name");
			Map(x => x.StreetName).Column("street_name");
			Map(x => x.HouseNumber).Column("house_number");
			Map(x => x.Latitude).Column("lat");
			Map(x => x.Longitude).Column("lon");
			Map(x => x.Reason).Column("reason");
		}
	}
}
