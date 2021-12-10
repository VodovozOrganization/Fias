using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	public class DistrictMap : ClassMap<District>
	{
		public DistrictMap()
		{
			Schema("public");
			Table("districts");
			Id(x => x.Id).Column("id").GeneratedBy.TriggerIdentity();
			Map(x => x.FiasCityGuid).Column("city_fias_guid");
			Map(x => x.Name).Column("name");
		}
	}
}
