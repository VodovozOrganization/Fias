using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class HouseCityHierarchy
	{
		public virtual Guid FiasHouseGuid { get; set; }
		public virtual Guid FiasCityGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is HouseCityHierarchy hierarchy &&
				   FiasHouseGuid == hierarchy.FiasHouseGuid &&
				   FiasCityGuid == hierarchy.FiasCityGuid;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasHouseGuid, FiasCityGuid);
		}
	}
}
