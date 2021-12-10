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
	public class HouseModel
	{
		private readonly HouseTypeModel _houseTypeModel;
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 1000;


		public HouseModel(HouseTypeModel houseTypeModel, FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_houseTypeModel = houseTypeModel ?? throw new ArgumentNullException(nameof(houseTypeModel));
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateHouses(int regionCode)
		{
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasHouse>(regionCode))
			{
				int processedCounter = 0;
				List<FiasHouse> batch = new List<FiasHouse>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasHouse = fiasReader.ReadNext();
					batch.Add(fiasHouse);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessFiasHouses(batch);
						processedCounter += batch.Count;
						Console.Write($"\rЗагрузка домов. Регион {regionCode}. Загружено {processedCounter}");
						batch = new List<FiasHouse>();
						loadCount = 0;
					}
				}
				ProcessFiasHouses(batch);
				processedCounter += batch.Count;
				Console.WriteLine($"\rЗагрузка домов. Регион {regionCode}. Загружено {processedCounter}");
			}
		}

		private void ProcessFiasHouses(IList<FiasHouse> fiasHouses)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var existingHouses = GetExistingHouses(fiasHouses);

				foreach(var fiasHouse in fiasHouses)
				{
					ProcessFiasHouse(session, fiasHouse, existingHouses);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessFiasHouse(ISession session, FiasHouse fiasHouse, IList<House> existedHouses)
		{
			var house = existedHouses.FirstOrDefault(x => x.FiasHouseId == fiasHouse.Id);
			if(house == null)
			{
				house = new House();
			}
			UpdateHouse(house, fiasHouse);
			session.SaveOrUpdate(house);
		}

		private void UpdateHouse(House house, FiasHouse fiasHouse)
		{
			house.FiasHouseId = fiasHouse.Id;
			house.FiasHouseGuid = new Guid(fiasHouse.ObjectGuid);
			house.PreviousId = fiasHouse.PreviousId;
			house.NextId = fiasHouse.NextId;
			house.UpdateDate = fiasHouse.UpdateDate;
			house.StartDate = fiasHouse.StartDate;
			house.EndDate = fiasHouse.EndDate;
			house.IsActive = fiasHouse.IsActive;
			house.IsActual = fiasHouse.IsActual;

			if(!string.IsNullOrWhiteSpace(fiasHouse.HouseNumber))
			{
				house.Number = fiasHouse.HouseNumber;
			}
			if(fiasHouse.HouseType > 0)
			{
				house.HouseType = _houseTypeModel.GetHouseType(fiasHouse.HouseType);
			}

			if(!string.IsNullOrWhiteSpace(fiasHouse.AddNumber1))
			{
				house.AddNumber1 = fiasHouse.AddNumber1;
			}
			if(fiasHouse.AddType1 > 0)
			{
				house.AddType1 = _houseTypeModel.GetHouseType(fiasHouse.AddType1);
			}

			if(!string.IsNullOrWhiteSpace(fiasHouse.AddNumber2))
			{
				house.AddNumber2 = fiasHouse.AddNumber2;
			}
			if(fiasHouse.AddType2 > 0)
			{
				house.AddType2 = _houseTypeModel.GetHouseType(fiasHouse.AddType2);
			}
		}

		private IList<House> GetExistingHouses(IList<FiasHouse> fiasHouses)
		{
			var houseFiasIds = fiasHouses
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var houses = session.QueryOver<House>()
					.WhereRestrictionOn(x => x.FiasHouseId).IsIn(houseFiasIds)
					.List();
				return houses;
			}
		}
	}
}
