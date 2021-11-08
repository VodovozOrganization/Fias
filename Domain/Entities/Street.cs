using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Street
    {
        public virtual int Id { get; set; }

        public virtual int FiasStreetId { get; set; }

        public virtual Guid FiasStreetGuid { get; set; }

        public virtual string Name { get; set; }

        public virtual ObjectLevel Level { get; set; }

        public virtual StreetType StreetType { get; set; }

        public virtual DateTime UpdateDate { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual bool IsActual { get; set; }

        public virtual bool IsUserData { get; set; }
    }
}
