﻿using Fias.Domain.Entities;
using FluentNHibernate.Mapping;

namespace Fias.Domain.Mapping
{
	class ApartmentTypeMap : ClassMap<ApartmentType>
	{
		public ApartmentTypeMap()
		{
			Table("apartment_types");
			Id(x => x.Id).Column("id").GeneratedBy.TriggerIdentity();
			Map(x => x.FiasId).Column("fias_id");
			Map(x => x.Name).Column("name");
			Map(x => x.ShortName).Column("short_name");
			Map(x => x.Description).Column("description");
			Map(x => x.UpdateDate).Column("update_date");
			Map(x => x.StartDate).Column("start_date");
			Map(x => x.EndDate).Column("end_date");
			Map(x => x.IsActive).Column("is_active");
		}
	}
}
