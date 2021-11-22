using System;

namespace Fias.Domain.Entities
{
	public class ObjectLevel
    {
        public virtual int Level { get; set; }
        public virtual string Name { get; set; }
        public virtual string ShortName { get; set; }
        public virtual DateTime UpdateDate { get; set; }
        public virtual DateTime StartDate { get; set; }
        public virtual DateTime EndDate { get; set; }
		public virtual bool IsActive { get; set; }
	}
}
