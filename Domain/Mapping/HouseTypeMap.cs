using Domain.Entities;
using FluentNHibernate.Mapping;

namespace Domain.Mapping
{
	public class HouseTypeMap : ClassMap<HouseType>
	{
		public HouseTypeMap()
		{
			Table("house_types");
			Id(x => x.Id).Column("id").GeneratedBy.Native();
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
