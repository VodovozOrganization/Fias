using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class OtherHierarchy
	{
		public virtual Guid FiasGuid { get; set; }
		public virtual Guid FiasParentGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is OtherHierarchy hierarchy &&
				   FiasGuid.Equals(hierarchy.FiasGuid) &&
				   FiasParentGuid.Equals(hierarchy.FiasParentGuid);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasGuid, FiasParentGuid);
		}
	}
}
