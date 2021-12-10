using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class StreetToStreetHierarchy
	{
		public virtual Guid FiasStreetGuid { get; set; }
		public virtual Guid FiasParentStreetGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is StreetToStreetHierarchy hierarchy &&
				   FiasStreetGuid.Equals(hierarchy.FiasStreetGuid) &&
				   FiasParentStreetGuid.Equals(hierarchy.FiasParentStreetGuid);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasStreetGuid, FiasParentStreetGuid);
		}
	}
}
