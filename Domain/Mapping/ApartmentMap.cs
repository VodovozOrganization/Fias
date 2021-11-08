using Domain.Entities;
using FluentNHibernate.Mapping;

namespace Domain.Mapping
{
	public class ApartmentMap : ClassMap<Apartment>
	{
		public ApartmentMap()
		{
			Table("apartments");
			Id(x => x.Id).Column("id").GeneratedBy.Native();
			Map(x => x.FiasApartmentId).Column("fias_apartment_id");
			Map(x => x.FiasApartmentGuid).Column("fias_apartment_guid");
			Map(x => x.Number).Column("number");
			References(x => x.ApartmentType).Column("apartment_type").Fetch.Join();
			Map(x => x.UpdateDate).Column("update_date");
			Map(x => x.StartDate).Column("start_date");
			Map(x => x.EndDate).Column("end_date");
			Map(x => x.IsActive).Column("is_active");
			Map(x => x.IsActual).Column("is_actual");
			Map(x => x.IsUserData).Column("is_user_data");
		}
	}
}
