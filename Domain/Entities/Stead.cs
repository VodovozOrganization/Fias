﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Stead
    {
        public virtual int Id { get; set; }

        public virtual int FiasSteadId { get; set; }

        public virtual Guid FiasSteadGuid { get; set; }

        public virtual string Number { get; set; }

        public virtual DateTime UpdateDate { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual bool IsActual { get; set; }

        public virtual bool IsUserData { get; set; }
    }
}
