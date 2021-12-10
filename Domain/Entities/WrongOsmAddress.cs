using System;
using System.Collections.Generic;
using System.Text;

namespace Fias.Domain.Entities
{
	public class WrongOsmAddress
	{
		public virtual int Id { get; set; }
		public virtual string CityName { get; set; }
		public virtual string StreetName { get; set; }
		public virtual string HouseNumber { get; set; }
		public virtual decimal Latitude { get; set; }
		public virtual decimal Longitude { get; set; }
		public virtual string Reason { get; set; }
	}
}
