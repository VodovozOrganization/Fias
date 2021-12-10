using System;

namespace Fias.Domain.Entities
{
	/// <summary>
	/// Район города
	/// </summary>
	public class District
	{
		public virtual int Id { get; set; }

		public virtual Guid FiasCityGuid { get; set; }

		public virtual string Name { get; set; }
	}
}
