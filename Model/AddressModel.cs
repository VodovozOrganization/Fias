using Fias.Domain.Entities;
using Fias.Source;
using Fias.Source.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Data;

namespace Fias.LoadModel
{
	public class AddressModel
	{
		private readonly AddressTypeModel _addressTypeModel;
		private readonly LevelModel _levelModel;
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 10;

		public AddressModel(AddressTypeModel addressTypeModel, LevelModel levelModel, FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_addressTypeModel = addressTypeModel ?? throw new ArgumentNullException(nameof(addressTypeModel));
			_levelModel = levelModel ?? throw new ArgumentNullException(nameof(levelModel));
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateAddressObjects(int regionCode)
		{
			if(regionCode <= 0)
			{
				throw new ArgumentException("Код региона должен быть указан", nameof(regionCode));
			}

			using(var fiasReader = _fiasReaderFactory.GetReader<FiasAddressObject>(regionCode))
			{
				int processedCounter = 0;
				List<FiasAddressObject> batch = new List<FiasAddressObject>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasAddressObject = fiasReader.ReadNext();
					batch.Add(fiasAddressObject);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessFiasAddressObjects(batch, regionCode);
						processedCounter += batch.Count;
						Console.Write($"\rЗагрузка городов и улиц. Регион {regionCode}. Загружено {processedCounter}");
						batch = new List<FiasAddressObject>();
						loadCount = 0;
					}
				}
				ProcessFiasAddressObjects(batch, regionCode);
				processedCounter += batch.Count;
				Console.WriteLine($"\rЗагрузка городов и улиц. Регион {regionCode}. Загружено {processedCounter}");
			}
		}

		private void ProcessFiasAddressObjects(IList<FiasAddressObject> fiasAddressObjects, int regionCode)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				foreach(var fiasAddressObject in fiasAddressObjects)
				{
					ProcessFiasAddress(session, fiasAddressObject, regionCode);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessFiasAddress(ISession session, FiasAddressObject fiasAddressObject, int regionCode)
		{
			var addressType =_addressTypeModel.GetAddressType(fiasAddressObject.TypeName, fiasAddressObject.Level);
			if(addressType is CityType)
			{
				ProcessCity(session, fiasAddressObject, (CityType)addressType, regionCode);
			}
			else if (addressType is StreetType)
			{
				ProcessStreet(session, fiasAddressObject, (StreetType)addressType);
			}
			else if(addressType is UncategorizedAddressObjectType)
			{
				ProcessUncategorizedObject(session, fiasAddressObject, (UncategorizedAddressObjectType)addressType);
			}
			else
			{
				throw new InvalidOperationException($"Неизвестный тип адреса {fiasAddressObject.TypeName}, {fiasAddressObject.Name}");
			}
		}

		#region City

		private void ProcessCity(ISession session, FiasAddressObject fiasAddressObject, CityType cityType, int regionCode)
		{
			var city = GetExistingCity(fiasAddressObject.Id);
			if(city == null)
			{
				city = new City();
			}
			UpdateCity(city, fiasAddressObject, cityType, regionCode);
			session.SaveOrUpdate(city);
		}

		private City GetExistingCity(long fiasId)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var city = session.QueryOver<City>()
					.Where(x => x.FiasCityId == fiasId)
					.SingleOrDefault();
				return city;
			}
		}

		private void UpdateCity(City city, FiasAddressObject fiasAddressObject, CityType cityType, int regionCode)
		{
			city.FiasCityId = fiasAddressObject.Id;
			city.FiasCityGuid = new Guid(fiasAddressObject.ObjectGuid);
			city.RegionCode = regionCode;
			city.Name = fiasAddressObject.Name;
			city.CityType = cityType;
			city.Level = _levelModel.GetLevel(fiasAddressObject.Level);
			city.PreviousId = fiasAddressObject.PreviousId;
			city.NextId = fiasAddressObject.NextId;
			city.UpdateDate = fiasAddressObject.UpdateDate;
			city.StartDate = fiasAddressObject.StartDate;
			city.EndDate = fiasAddressObject.EndDate;
			city.IsActive = fiasAddressObject.IsActive;
			city.IsActual = fiasAddressObject.IsActual;
		}

		#endregion

		#region Street

		private void ProcessStreet(ISession session, FiasAddressObject fiasAddressObject, StreetType addressType)
		{
			var street = GetExistingStreet(fiasAddressObject.Id);
			if(street == null)
			{
				street = new Street();
			}
			UpdateStreet(street, fiasAddressObject, addressType);
			session.SaveOrUpdate(street);
		}

		private Street GetExistingStreet(long fiasId)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var street = session.QueryOver<Street>()
					.Where(x => x.FiasStreetId == fiasId)
					.SingleOrDefault();
				return street;
			}
		}

		private void UpdateStreet(Street street, FiasAddressObject fiasAddressObject, StreetType streetType)
		{
			street.FiasStreetId = fiasAddressObject.Id;
			street.FiasStreetGuid = new Guid(fiasAddressObject.ObjectGuid);
			street.Name = fiasAddressObject.Name;
			street.StreetType = streetType;
			street.Level = _levelModel.GetLevel(fiasAddressObject.Level);
			street.PreviousId = fiasAddressObject.PreviousId;
			street.NextId = fiasAddressObject.NextId;
			street.UpdateDate = fiasAddressObject.UpdateDate;
			street.StartDate = fiasAddressObject.StartDate;
			street.EndDate = fiasAddressObject.EndDate;
			street.IsActive = fiasAddressObject.IsActive;
			street.IsActual = fiasAddressObject.IsActual;
		}

		#endregion

		#region UncategorizedObject

		private void ProcessUncategorizedObject(ISession session, FiasAddressObject fiasAddressObject, UncategorizedAddressObjectType addressType)
		{
			var uncategorizedObject = GetExistingUncategorizedObject(fiasAddressObject.Id);
			if(uncategorizedObject == null)
			{
				uncategorizedObject = new UncategorizedAddressObject();
			}
			UpdateUncategorizedObject(uncategorizedObject, fiasAddressObject, addressType);
			session.SaveOrUpdate(uncategorizedObject);
		}

		private UncategorizedAddressObject GetExistingUncategorizedObject(long fiasId)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var uncategorizedObject = session.QueryOver<UncategorizedAddressObject>()
					.Where(x => x.FiasId == fiasId)
					.SingleOrDefault();
				return uncategorizedObject;
			}
		}

		private void UpdateUncategorizedObject(UncategorizedAddressObject uncategorizedObject, FiasAddressObject fiasAddressObject, UncategorizedAddressObjectType uncategorizedAddressObjectType)
		{
			uncategorizedObject.FiasId = fiasAddressObject.Id;
			uncategorizedObject.FiasGuid = new Guid(fiasAddressObject.ObjectGuid);
			uncategorizedObject.Name = fiasAddressObject.Name;
			uncategorizedObject.ObjectType = uncategorizedAddressObjectType;
			uncategorizedObject.Level = _levelModel.GetLevel(fiasAddressObject.Level);
			uncategorizedObject.PreviousId = fiasAddressObject.PreviousId;
			uncategorizedObject.NextId = fiasAddressObject.NextId;
			uncategorizedObject.UpdateDate = fiasAddressObject.UpdateDate;
			uncategorizedObject.StartDate = fiasAddressObject.StartDate;
			uncategorizedObject.EndDate = fiasAddressObject.EndDate;
			uncategorizedObject.IsActive = fiasAddressObject.IsActive;
			uncategorizedObject.IsActual = fiasAddressObject.IsActual;
		}

		#endregion
	}
}
