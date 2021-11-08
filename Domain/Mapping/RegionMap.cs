using Domain.Entities;
using FluentNHibernate.Mapping;

namespace Domain.Mapping
{
	public class RegionMap : ClassMap<Region>
	{
		public RegionMap()
		{
			Table("regions");
			Id(x => x.Code).Column("code");
			Map(x => x.Name).Column("name");
		}
	}
}
