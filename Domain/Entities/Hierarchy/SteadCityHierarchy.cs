using System;

namespace Fias.Domain.Entities.Hierarchy
{
	[Serializable]
	public class SteadCityHierarchy
	{
		public virtual Guid FiasSteadGuid { get; set; }
		public virtual Guid FiasCityGuid { get; set; }

		public override bool Equals(object obj)
		{
			return obj is SteadCityHierarchy hierarchy &&
				   FiasSteadGuid == hierarchy.FiasSteadGuid &&
				   FiasCityGuid == hierarchy.FiasCityGuid;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(FiasSteadGuid, FiasCityGuid);
		}
	}
}
