using Fias.Domain.Entities;
using Fias.Source;
using Fias.Source.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Fias.LoadModel
{
	public class ApartmentModel
	{
		private readonly ApartmentTypeModel _apartmentTypeModel;
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 1000;


		public ApartmentModel(ApartmentTypeModel apartmentTypeModel, FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_apartmentTypeModel = apartmentTypeModel ?? throw new ArgumentNullException(nameof(apartmentTypeModel));
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateApartments(int regionCode)
		{
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasApartment>(regionCode))
			{
				int processedCounter = 0;
				List<FiasApartment> batch = new List<FiasApartment>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasApartment = fiasReader.ReadNext();
					batch.Add(fiasApartment);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessFiasApartments(batch);
						processedCounter += batch.Count;
						Console.Write($"\rЗагрузка помещений. Регион {regionCode}. Загружено {processedCounter}");
						batch = new List<FiasApartment>();
						loadCount = 0;
					}
				}
				ProcessFiasApartments(batch);
				processedCounter += batch.Count;
				Console.WriteLine($"\rЗагрузка помещений. Регион {regionCode}. Загружено {processedCounter}");
			}
		}

		private void ProcessFiasApartments(IList<FiasApartment> fiasApartments)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var existedApartments = GetExistedApartments(fiasApartments);

				foreach(var fiasApartment in fiasApartments)
				{
					ProcessFiasApartment(session, fiasApartment, existedApartments);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessFiasApartment(ISession session, FiasApartment fiasApartment, IList<Apartment> existedApartments)
		{
			var apartment = existedApartments.FirstOrDefault(x => x.FiasApartmentId == fiasApartment.Id);
			if(apartment == null)
			{
				apartment = new Apartment();
			}

			UpdateApartment(apartment, fiasApartment);
			session.SaveOrUpdate(apartment);
		}

		private void UpdateApartment(Apartment apartment, FiasApartment fiasApartment)
		{
			var apartmentType = fiasApartment.ApartmentType == 0 ? 1 : fiasApartment.ApartmentType;

			apartment.FiasApartmentId = fiasApartment.Id;
			apartment.FiasApartmentGuid = new Guid(fiasApartment.ObjectGuid);
			apartment.Number = fiasApartment.Number;
			apartment.ApartmentType = _apartmentTypeModel.GetApartmentType(apartmentType);
			apartment.PreviousId = fiasApartment.PreviousId;
			apartment.NextId = fiasApartment.NextId;
			apartment.UpdateDate = fiasApartment.UpdateDate;
			apartment.StartDate = fiasApartment.StartDate;
			apartment.EndDate = fiasApartment.EndDate;
			apartment.IsActive = fiasApartment.IsActive;
			apartment.IsActual = fiasApartment.IsActual;
		}

		private IList<Apartment> GetExistedApartments(IList<FiasApartment> fiasApartments)
		{
			var apartmentFiasIds = fiasApartments
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var apartments = session.QueryOver<Apartment>()
					.WhereRestrictionOn(x => x.FiasApartmentId).IsIn(apartmentFiasIds)
					.List();
				return apartments;
			}
		}
	}
}
