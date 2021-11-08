﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class House
    {
        public virtual int Id { get; set; }

        public virtual int FiasHouseId { get; set; }

        public virtual Guid FiasHouseGuid { get; set; }

        public virtual string Number { get; set; }

        public virtual HouseType HouseType { get; set; }

        public virtual string AddNumber1 { get; set; }

        public virtual HouseType AddType1 { get; set; }

        public virtual string AddNumber2 { get; set; }

        public virtual HouseType AddType2 { get; set; }

        public virtual DateTime UpdateDate { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual bool IsActual { get; set; }

        public virtual bool IsUserData { get; set; }
    }
}
