using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
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
