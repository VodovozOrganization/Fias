using Domain.Entities;
using FluentNHibernate.Mapping;

namespace Domain.Mapping
{
	public class LevelMap : ClassMap<ObjectLevel>
	{
		public LevelMap()
		{
			Table("levels");
			Id(x => x.Level).Column("level");
			Map(x => x.Name).Column("name");
		}
	}
}
