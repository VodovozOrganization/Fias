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
	public class ApartmentTypeModel
	{
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly List<ApartmentType> _apartmentTypeCache;
		private readonly int _batchSize = 10;


		public ApartmentTypeModel(FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
			_apartmentTypeCache = new List<ApartmentType>();
		}

		public void LoadAndUpdateApartmentTypes()
		{
			Console.WriteLine("Загрузка справочника типов помещений.");
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasApartmentType>())
			{
				List<FiasApartmentType> batch = new List<FiasApartmentType>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasApartmentType = fiasReader.ReadNext();
					batch.Add(fiasApartmentType);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessFiasApartmentTypes(batch);
						batch = new List<FiasApartmentType>();
						loadCount = 0;
					}
				}
				ProcessFiasApartmentTypes(batch);
			}
		}

		private void ProcessFiasApartmentTypes(IList<FiasApartmentType> fiasApartmentTypes)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var existedApartmentTypes = GetExistedApartmentTypes(fiasApartmentTypes);

				foreach(var fiasApartmentType in fiasApartmentTypes)
				{
					ProcessFiasApartmentType(session, fiasApartmentType, existedApartmentTypes);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessFiasApartmentType(ISession session, FiasApartmentType fiasApartmentType, IList<ApartmentType> existedApartmentTypes)
		{
			var apartmentType = existedApartmentTypes.FirstOrDefault(x => x.FiasId == fiasApartmentType.Id);
			if(apartmentType == null)
			{
				apartmentType = new ApartmentType();
			}

			UpdateApartmentType(apartmentType, fiasApartmentType);
			session.SaveOrUpdate(apartmentType);
			_apartmentTypeCache.Add(apartmentType);
		}

		private void UpdateApartmentType(ApartmentType apartmentType, FiasApartmentType fiasApartmentType)
		{
			apartmentType.FiasId = fiasApartmentType.Id;
			apartmentType.Name = fiasApartmentType.Name;
			apartmentType.ShortName = fiasApartmentType.ShortName;
			apartmentType.Description = fiasApartmentType.Description;
			apartmentType.UpdateDate = fiasApartmentType.UpdateDate;
			apartmentType.StartDate = fiasApartmentType.StartDate;
			apartmentType.EndDate = fiasApartmentType.EndDate;
			apartmentType.IsActive = fiasApartmentType.IsActive;
		}

		private IList<ApartmentType> GetExistedApartmentTypes(IList<FiasApartmentType> fiasApartmentTypes)
		{
			var apartmentTypeFiasIds = fiasApartmentTypes
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var apartmentTypes = session.QueryOver<ApartmentType>()
					.WhereRestrictionOn(x => x.FiasId).IsIn(apartmentTypeFiasIds)
					.List();
				return apartmentTypes;
			}
		}

		public ApartmentType GetApartmentType(int fiasId)
		{
			var apartmentType = _apartmentTypeCache.FirstOrDefault(x => x.FiasId == fiasId);
			if(apartmentType == null)
			{
				throw new InvalidOperationException($"Невозможно найти тип дома по FiasId ({fiasId}). Возможно типы домов не были загружены или передан не правильный FiasId.");
			}
			return apartmentType;
		}
	}
}
