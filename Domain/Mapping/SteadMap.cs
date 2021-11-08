using Domain.Entities;
using FluentNHibernate.Mapping;

namespace Domain.Mapping
{
	public class SteadMap : ClassMap<Stead>
	{
		public SteadMap()
		{
			Table("steads");
			Id(x => x.Id).Column("id").GeneratedBy.Native();
			Map(x => x.FiasSteadId).Column("fias_stead_id");
			Map(x => x.FiasSteadGuid).Column("fias_stead_guid");
			Map(x => x.Number).Column("number");
			Map(x => x.UpdateDate).Column("update_date");
			Map(x => x.StartDate).Column("start_date");
			Map(x => x.EndDate).Column("end_date");
			Map(x => x.IsActive).Column("is_active");
			Map(x => x.IsActual).Column("is_actual");
			Map(x => x.IsUserData).Column("is_user_data");
		}
	}
}
