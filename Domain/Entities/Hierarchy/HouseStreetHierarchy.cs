using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class HouseStreetHierarchy
	{
		public virtual Guid FiasHouseGuid { get; set; }
		public virtual Guid FiasStreetGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is HouseStreetHierarchy hierarchy &&
				   FiasHouseGuid == hierarchy.FiasHouseGuid &&
				   FiasStreetGuid == hierarchy.FiasStreetGuid;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasHouseGuid, FiasStreetGuid);
		}
	}
}
