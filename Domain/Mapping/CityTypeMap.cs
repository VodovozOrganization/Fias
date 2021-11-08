using Domain.Entities;
using FluentNHibernate.Mapping;

namespace Domain.Mapping
{
	public class CityTypeMap : ClassMap<CityType>
	{
		public CityTypeMap()
		{
			Table("city_types");
			Id(x => x.Id).Column("id").GeneratedBy.Native();
			References(x => x.Level).Column("level").Fetch.Join();
			Map(x => x.Name).Column("name");
			Map(x => x.ShortName).Column("short_name");
			Map(x => x.Description).Column("description");
			Map(x => x.UpdateDate).Column("update_date");
			Map(x => x.StartDate).Column("start_date");
			Map(x => x.EndDate).Column("end_date");
			Map(x => x.IsActive).Column("is_active");
		}
	}
}
