using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class CityHierarchy
	{
		public virtual Guid FiasCityGuid { get; set; }
		public virtual Guid FiasParentObjectGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is CityHierarchy hierarchy &&
				   FiasCityGuid == hierarchy.FiasCityGuid &&
				   FiasParentObjectGuid == hierarchy.FiasParentObjectGuid;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasCityGuid, FiasParentObjectGuid);
		}
	}
}
