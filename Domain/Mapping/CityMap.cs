using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	public class CityMap : ClassMap<City>
	{
		public CityMap()
		{
			Table("cities");
			Id(x => x.Id).Column("id").GeneratedBy.TriggerIdentity();
			Map(x => x.RegionCode).Column("region_code");
			Map(x => x.FiasCityId).Column("fias_city_id");
			Map(x => x.FiasCityGuid).Column("fias_city_guid");
			Map(x => x.Name).Column("name");
			References(x => x.Level).Column("level").Fetch.Join();
			References(x => x.CityType).Column("type_id").Fetch.Join();
			Map(x => x.PreviousId).Column("previous_id");
			Map(x => x.NextId).Column("next_id");
			Map(x => x.UpdateDate).Column("update_date");
			Map(x => x.StartDate).Column("start_date");
			Map(x => x.EndDate).Column("end_date");
			Map(x => x.IsActive).Column("is_active");
			Map(x => x.IsActual).Column("is_actual");
			Map(x => x.IsUserData).Column("is_user_data");
		}
	}
}
