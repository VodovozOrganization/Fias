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
	public class HouseTypeModel
	{
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly List<HouseType> _houseTypeCache;
		private readonly int _batchSize = 10;


		public HouseTypeModel(FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
			_houseTypeCache = new List<HouseType>();
		}

		public void LoadAndUpdateHouseTypes()
		{
			Console.WriteLine("Загрузка справочника типов домов.");
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasHouseType>())
			{
				List<FiasHouseType> batch = new List<FiasHouseType>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasHouseType = fiasReader.ReadNext();
					batch.Add(fiasHouseType);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessFiasHouseTypes(batch);
						batch = new List<FiasHouseType>();
						loadCount = 0;
					}
				}
				ProcessFiasHouseTypes(batch);
			}
		}

		private void ProcessFiasHouseTypes(IList<FiasHouseType> fiasHouseTypes)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var existedHouseTypes = GetExistedHouseTypes(fiasHouseTypes);

				foreach(var fiasHouseType in fiasHouseTypes)
				{
					ProcessFiasHouseType(session, fiasHouseType, existedHouseTypes);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessFiasHouseType(ISession session, FiasHouseType fiasHouseType, IList<HouseType> existedHouseTypes)
		{
			var houseType = existedHouseTypes.FirstOrDefault(x => x.FiasId == fiasHouseType.Id);
			if(houseType == null)
			{
				houseType = new HouseType();
			}

			UpdateHouseType(houseType, fiasHouseType);
			session.SaveOrUpdate(houseType);
			_houseTypeCache.Add(houseType);
		}

		private void UpdateHouseType(HouseType houseType, FiasHouseType fiasHouseType)
		{
			houseType.FiasId = fiasHouseType.Id;
			houseType.Name = fiasHouseType.Name;
			houseType.ShortName = fiasHouseType.ShortName;
			houseType.Description = fiasHouseType.Description;
			houseType.UpdateDate = fiasHouseType.UpdateDate;
			houseType.StartDate = fiasHouseType.StartDate;
			houseType.EndDate = fiasHouseType.EndDate;
			houseType.IsActive = fiasHouseType.IsActive;
		}

		private IList<HouseType> GetExistedHouseTypes(IList<FiasHouseType> fiasHouseTypes)
		{
			var houseTypeFiasIds = fiasHouseTypes
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var houseTypes = session.QueryOver<HouseType>()
					.WhereRestrictionOn(x => x.FiasId).IsIn(houseTypeFiasIds)
					.List();
				return houseTypes;
			}
		}

		public HouseType GetHouseType(int fiasId)
		{
			var houseType = _houseTypeCache.FirstOrDefault(x => x.FiasId == fiasId);
			if(houseType == null)
			{
				throw new InvalidOperationException($"Невозможно найти тип дома по FiasId ({fiasId}). Возможно типы домов не были загружены или передан не правильный FiasId.");
			}
			return houseType;
		}
	}
}
