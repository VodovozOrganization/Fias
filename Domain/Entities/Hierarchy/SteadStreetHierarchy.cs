using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class SteadStreetHierarchy
	{
		public virtual Guid FiasSteadGuid { get; set; }
		public virtual Guid FiasStreetGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is SteadStreetHierarchy hierarchy &&
				   FiasSteadGuid == hierarchy.FiasSteadGuid &&
				   FiasStreetGuid == hierarchy.FiasStreetGuid;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasSteadGuid, FiasStreetGuid);
		}
	}
}
