using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	public class MunDistrictMap : ClassMap<MunDistrict>
	{
		public MunDistrictMap()
		{
			Schema("public");
			Table("mun_districts");
			Id(x => x.Id).Column("id").GeneratedBy.TriggerIdentity();
			References(x => x.District).Column("district_id").Fetch.Join().Not.LazyLoad();
			Map(x => x.FiasId).Column("mun_district_fias_id");
			Map(x => x.FiasGuid).Column("mun_district_fias_guid");
			Map(x => x.Name).Column("name");
			References(x => x.Level).Column("level").Fetch.Join().Not.LazyLoad();
			References(x => x.ObjectType).Column("type_id").Fetch.Join().Not.LazyLoad();
		}
	}
}
