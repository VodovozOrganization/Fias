using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class StreetOtherHierarchy
	{
		public virtual Guid FiasStreetGuid { get; set; }
		public virtual Guid FiasParentGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is StreetOtherHierarchy hierarchy &&
				   FiasStreetGuid.Equals(hierarchy.FiasStreetGuid) &&
				   FiasParentGuid.Equals(hierarchy.FiasParentGuid);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasStreetGuid, FiasParentGuid);
		}
	}
}

