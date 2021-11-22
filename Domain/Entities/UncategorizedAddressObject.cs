using System;

namespace Fias.Domain.Entities
{
	public class UncategorizedAddressObject
	{
		public virtual int Id { get; set; }

		public virtual long FiasId { get; set; }

		public virtual Guid FiasGuid { get; set; }

		public virtual string Name { get; set; }

		public virtual ObjectLevel Level { get; set; }

		public virtual UncategorizedAddressObjectType ObjectType { get; set; }

		public virtual long PreviousId { get; set; }

		public virtual long NextId { get; set; }

		public virtual DateTime UpdateDate { get; set; }

		public virtual DateTime StartDate { get; set; }

		public virtual DateTime EndDate { get; set; }

		public virtual bool IsActive { get; set; }

		public virtual bool IsActual { get; set; }

		public virtual bool IsUserData { get; set; }
	}
}
