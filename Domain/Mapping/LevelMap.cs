using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	public class LevelMap : ClassMap<ObjectLevel>
	{
		public LevelMap()
		{
			Schema("public");
			Table("levels");
			Id(x => x.Level).Column("level").GeneratedBy.Assigned();
			Map(x => x.Name).Column("name");
			Map(x => x.ShortName).Column("short_name");
			Map(x => x.UpdateDate).Column("update_date");
			Map(x => x.StartDate).Column("start_date");
			Map(x => x.EndDate).Column("end_date");
			Map(x => x.IsActive).Column("is_active");
		}
	}
}
