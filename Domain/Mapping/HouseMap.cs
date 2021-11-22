using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	public class HouseMap : ClassMap<House>
	{
		public HouseMap()
		{
			Table("houses");
			Id(x => x.Id).Column("id").GeneratedBy.TriggerIdentity();
			Map(x => x.FiasHouseId).Column("fias_house_id");
			Map(x => x.FiasHouseGuid).Column("fias_house_guid");
			Map(x => x.Number).Column("number");
			References(x => x.HouseType).Column("house_type").Fetch.Join();
			Map(x => x.AddNumber1).Column("add_number_1");
			References(x => x.AddType1).Column("add_type_1").Fetch.Join();
			Map(x => x.AddNumber2).Column("add_number_2");
			References(x => x.AddType2).Column("add_type_2").Fetch.Join();
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
