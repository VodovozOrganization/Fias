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
	public class AddressTypeModel
	{
		private readonly LevelModel _levelModel;
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly List<CityType> _cityTypeCache;
		private readonly List<StreetType> _streetTypeCache;
		private readonly List<UncategorizedAddressObjectType> _uncategorizedTypeCache;
		private readonly int _batchSize = 10;
		private readonly int[] _cityLevels = new[] { 4, 5, 6 };
		private readonly int[] _streetLevels = new[] { 7, 8, 15, 16 };
		private long[] _exactCityTypeIds = { 6, 7, 8, 423 };


		public AddressTypeModel(LevelModel levelModel, FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_levelModel = levelModel ?? throw new ArgumentNullException(nameof(levelModel));
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
			_cityTypeCache = new List<CityType>();
			_streetTypeCache = new List<StreetType>();
			_uncategorizedTypeCache = new List<UncategorizedAddressObjectType>();
		}

		public void LoadAndUpdateAddressObjectTypes()
		{
			Console.WriteLine("Загрузка справочника типов городов и улиц.");
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasAddressObjectType>())
			{
				List<FiasAddressObjectType> batch = new List<FiasAddressObjectType>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasAddressType = fiasReader.ReadNext();
					batch.Add(fiasAddressType);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessAddressTypes(batch);
						batch = new List<FiasAddressObjectType>();
						loadCount = 0;
					}
				}
				ProcessAddressTypes(batch);
			}
		}

		private void ProcessAddressTypes(IList<FiasAddressObjectType> fiasAddressObjectTypes)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var existingCityTypes = GetExistingCityTypes(fiasAddressObjectTypes);
				var existingStreetTypes = GetExistingStreetTypes(fiasAddressObjectTypes);
				var existingUncategorizedAddressObjectTypes = GetExistedUncategorizedAddressObjectTypes(fiasAddressObjectTypes);

				foreach(var fiasAddressObjectType in fiasAddressObjectTypes)
				{
					if(IsCity(fiasAddressObjectType))
					{
						ProcessFiasCityType(session, fiasAddressObjectType, existingCityTypes);
					}
					else if(_streetLevels.Contains(fiasAddressObjectType.Level))
					{
						ProcessFiasStreetType(session, fiasAddressObjectType, existingStreetTypes);
					}
					else
					{
						ProcessUncategorizedAddressObjectType(session, fiasAddressObjectType, existingUncategorizedAddressObjectTypes);
					}
				}
				session.Flush();
				transaction.Commit();
			}
		}

		private bool IsCity(FiasAddressObjectType fiasAddressObjectType)
		{
			if(_cityLevels.Contains(fiasAddressObjectType.Level))
			{
				return true;
			}

			return _exactCityTypeIds.Contains(fiasAddressObjectType.Id);
		}

		private IList<CityType> GetExistingCityTypes(IList<FiasAddressObjectType> fiasAddressObjectTypes)
		{
			var cityTypeFiasIds = fiasAddressObjectTypes
				.Where(x => _cityLevels.Contains(x.Level) || _exactCityTypeIds.Contains(x.Id))
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var cityTypes = session.QueryOver<CityType>()
					.WhereRestrictionOn(x => x.FiasId).IsIn(cityTypeFiasIds)
					.List();
				return cityTypes;
			}
		}

		private IList<StreetType> GetExistingStreetTypes(IList<FiasAddressObjectType> fiasAddressObjectTypes)
		{
			var streetTypeFiasIds = fiasAddressObjectTypes
				.Where(x => _streetLevels.Contains(x.Level))
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var streetTypes = session.QueryOver<StreetType>()
					.WhereRestrictionOn(x => x.FiasId).IsIn(streetTypeFiasIds)
					.List();
				return streetTypes;
			}
		}

		private IList<UncategorizedAddressObjectType> GetExistedUncategorizedAddressObjectTypes(IList<FiasAddressObjectType> fiasAddressObjectTypes)
		{
			var uncategorizedTypeFiasIds = fiasAddressObjectTypes
				.Where(x => !_cityLevels.Contains(x.Level))
				.Where(x => !_streetLevels.Contains(x.Level))
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var uncategorizedAddressObjectType = session.QueryOver<UncategorizedAddressObjectType>()
					.WhereRestrictionOn(x => x.FiasId).IsIn(uncategorizedTypeFiasIds)
					.List();
				return uncategorizedAddressObjectType;
			}
		}

		private void ProcessFiasCityType(ISession session, FiasAddressObjectType fiasAddressObjectType, IList<CityType> existedCityTypes)
		{
			var cityType = existedCityTypes.FirstOrDefault(x => x.FiasId == fiasAddressObjectType.Id);
			if(cityType == null)
			{
				cityType = new CityType();
			}

			UpdateAddressObjectType(cityType, fiasAddressObjectType);
			session.SaveOrUpdate(cityType);
			_cityTypeCache.Add(cityType);
		}

		private void ProcessFiasStreetType(ISession session, FiasAddressObjectType fiasAddressObjectType, IList<StreetType> existedStreetTypes)
		{
			var streetType = existedStreetTypes.FirstOrDefault(x => x.FiasId == fiasAddressObjectType.Id);
			if(streetType == null)
			{
				streetType = new StreetType();
			}

			UpdateAddressObjectType(streetType, fiasAddressObjectType);
			session.SaveOrUpdate(streetType);
			_streetTypeCache.Add(streetType);
		}

		private void ProcessUncategorizedAddressObjectType(ISession session, FiasAddressObjectType fiasAddressObjectType, 
			IList<UncategorizedAddressObjectType> uncategorizedAddressObjectTypes)
		{
			var uncategorizedType = uncategorizedAddressObjectTypes.FirstOrDefault(x => x.FiasId == fiasAddressObjectType.Id);
			if(uncategorizedType == null)
			{
				uncategorizedType = new UncategorizedAddressObjectType();
			}

			UpdateAddressObjectType(uncategorizedType, fiasAddressObjectType);
			session.SaveOrUpdate(uncategorizedType);
			_uncategorizedTypeCache.Add(uncategorizedType);
		}

		private void UpdateAddressObjectType(AddressObjectType addressObjectType, FiasAddressObjectType fiasAddressObjectType)
		{
			addressObjectType.FiasId = fiasAddressObjectType.Id;
			addressObjectType.Name = fiasAddressObjectType.Name;
			addressObjectType.ShortName = fiasAddressObjectType.ShortName;
			addressObjectType.Description = fiasAddressObjectType.Description;
			addressObjectType.UpdateDate = fiasAddressObjectType.UpdateDate;
			addressObjectType.StartDate = fiasAddressObjectType.StartDate;
			addressObjectType.EndDate = fiasAddressObjectType.EndDate;
			addressObjectType.IsActive = fiasAddressObjectType.IsActive;
			
			if(addressObjectType.Level == null || addressObjectType.Level.Level != fiasAddressObjectType.Level)
			{
				addressObjectType.Level = _levelModel.GetLevel(fiasAddressObjectType.Level);
			}
		}

		public CityType GetCityType(int fiasId)
		{
			var cityType = _cityTypeCache.FirstOrDefault(x => x.FiasId == fiasId);
			if(cityType == null)
			{
				throw new InvalidOperationException($"Невозможно найти тип города по FiasId ({fiasId}). Возможно типы городов не были загружены или передан не правильный FiasId.");
			}
			return cityType;
		}

		public StreetType GetStreetType(int fiasId)
		{
			var streetType = _streetTypeCache.FirstOrDefault(x => x.FiasId == fiasId);
			if(streetType == null)
			{
				throw new InvalidOperationException($"Невозможно найти тип города по FiasId ({fiasId}). Возможно типы городов не были загружены или передан не правильный FiasId.");
			}
			return streetType;
		}


		public AddressObjectType GetAddressType(string shortTypeName, int level)
		{
			AddressObjectType addressType = _cityTypeCache
				.Where(x => x.Level.Level == level)
				.Where(x => x.ShortName == shortTypeName)
				.SingleOrDefault();

			if(addressType == null)
			{
				addressType = _streetTypeCache
					.Where(x => x.Level.Level == level)
					.Where(x => x.ShortName == shortTypeName)
					.SingleOrDefault();
			}

			if(addressType == null)
			{
				addressType = _uncategorizedTypeCache
					.Where(x => x.Level.Level == level)
					.Where(x => x.ShortName == shortTypeName)
					.SingleOrDefault();
			}

			if(addressType == null)
			{
				throw new InvalidOperationException($"Не известный тип адреса. shortname: {shortTypeName}, level: {level}");
			}

			return addressType;
		}
	}
}
