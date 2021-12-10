using System;

namespace Fias.Domain.Entities
{
	public class HouseCoordinate
	{
		public virtual Guid HouseFiasGuid { get; set; }
		public virtual decimal Latitude { get; set; }
		public virtual decimal Longitude { get; set; }
	}
}
