using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	class ReestrObjectMap : ClassMap<ReestrObject>
	{
		public ReestrObjectMap()
		{
			Table("reestr_objects");
			Id(x => x.Id).Column("id").GeneratedBy.Assigned();
			References(x => x.Level).Column("level").Fetch.Join();
			Map(x => x.FiasObjectGuid).Column("fias_object_guid");
			Map(x => x.ChangeId).Column("change_id");
			Map(x => x.CreateDate).Column("create_date");
			Map(x => x.UpdateDate).Column("update_date");
			Map(x => x.IsActive).Column("is_active");
		}
	}
}
