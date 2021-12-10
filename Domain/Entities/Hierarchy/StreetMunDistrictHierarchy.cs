using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class StreetMunDistrictHierarchy
	{
		public virtual Guid FiasStreetGuid { get; set; }
		public virtual Guid FiasMunDistrictGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is StreetMunDistrictHierarchy hierarchy &&
				   FiasMunDistrictGuid.Equals(hierarchy.FiasMunDistrictGuid);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasMunDistrictGuid);
		}
	}
}

