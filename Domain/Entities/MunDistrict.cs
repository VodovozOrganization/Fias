using System;

namespace Fias.Domain.Entities
{
	/// <summary>
	/// Муниципальный округ
	/// </summary>
	public class MunDistrict
	{
		public virtual int Id { get; set; }

		public virtual District District { get; set; }

		public virtual long FiasId { get; set; }

		public virtual Guid FiasGuid { get; set; }

		public virtual string Name { get; set; }

		public virtual ObjectLevel Level { get; set; }

		public virtual UncategorizedAddressObjectType ObjectType { get; set; }
	}
}
