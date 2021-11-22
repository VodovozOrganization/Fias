using System;

namespace Fias.Domain.Entities
{
    public class Apartment
    {
        public virtual int Id { get; set; }

        public virtual long FiasApartmentId { get; set; }

        public virtual Guid FiasApartmentGuid { get; set; }

        public virtual string Number { get; set; }

        public virtual ApartmentType ApartmentType { get; set; }

        public virtual DateTime UpdateDate { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }

		public virtual long PreviousId { get; set; }

		public virtual long NextId { get; set; }

		public virtual bool IsActive { get; set; }

        public virtual bool IsActual { get; set; }

        public virtual bool IsUserData { get; set; }
    }
}
