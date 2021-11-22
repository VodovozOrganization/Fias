using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class StreetCityHierarchy
	{
		public virtual Guid FiasStreetGuid { get; set; }
		public virtual Guid FiasCityGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is StreetCityHierarchy hierarchy &&
				   FiasStreetGuid.Equals(hierarchy.FiasStreetGuid) &&
				   FiasCityGuid.Equals(hierarchy.FiasCityGuid);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasStreetGuid, FiasCityGuid);
		}
	}
}
