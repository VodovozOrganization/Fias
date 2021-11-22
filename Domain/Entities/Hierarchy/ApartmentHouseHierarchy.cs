using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class ApartmentHouseHierarchy
	{
		public virtual Guid FiasApartmentGuid { get; set; }
		public virtual Guid FiasHouseGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is ApartmentHouseHierarchy hierarchy &&
				   FiasApartmentGuid == hierarchy.FiasApartmentGuid &&
				   FiasHouseGuid == hierarchy.FiasHouseGuid;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasApartmentGuid, FiasHouseGuid);
		}
	}
}
