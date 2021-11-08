using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class StreetType
    {
        public virtual int Id { get; set; }

        public virtual ObjectLevel Level { get; set; }

        public virtual string Name { get; set; }

        public virtual string ShortName { get; set; }

        public virtual string Description { get; set; }

        public virtual DateTime UpdateDate { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        public virtual bool IsActive { get; set; }
    }
}
