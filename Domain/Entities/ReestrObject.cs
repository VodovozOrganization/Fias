using System;

namespace Fias.Domain.Entities
{
	public class ReestrObject
	{
		public virtual long Id { get; set; }

		public virtual ObjectLevel Level { get; set; }

		public virtual Guid FiasObjectGuid { get; set; }

		public virtual long ChangeId { get; set; }

		public virtual DateTime CreateDate { get; set; }

		public virtual DateTime UpdateDate { get; set; }

		public virtual bool IsActive { get; set; }
	}
}
