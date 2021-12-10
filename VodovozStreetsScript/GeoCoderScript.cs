using NHibernate;
using System;
using System.Linq;
using Dadata;
using Dadata.Model;
using System.Threading.Tasks;
using Fias.Search;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Collections.Generic;
using NHibernate.Transform;
using System.Data;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Net.Http.Headers;

namespace VodovozStreetsScript
{
	public class GeoCoderScript
	{
		private readonly ISessionFactory _fiasSessionFactory;
		private readonly ISessionFactory _vodovozSessionFactory;
		private readonly string _apiToken;
		private readonly string _secretToken;
		private readonly int _idFrom;
		private readonly int _idTo;
		private readonly StreetRepository _streetRepository;
		private readonly CityRepository _cityRepository;
		private readonly IEnumerable<DadataToken> _dadataTokens;

		public GeoCoderScript(ISessionFactory fiasSessionFactory, ISessionFactory vodovozSessionFactory, string apiToken, string secretToken, int idFrom, int IdTo)
		{
			_fiasSessionFactory = fiasSessionFactory ?? throw new ArgumentNullException(nameof(fiasSessionFactory));
			_vodovozSessionFactory = vodovozSessionFactory ?? throw new ArgumentNullException(nameof(vodovozSessionFactory));
			_apiToken = apiToken;
			_secretToken = secretToken;
			_idFrom = idFrom;
			_idTo = IdTo;
			_streetRepository = new StreetRepository(_fiasSessionFactory);
			_cityRepository = new CityRepository(_fiasSessionFactory);
		}


		public void Start()
		{
			var requestLimit = 9000 - GetRequestsStatistic(_apiToken, _secretToken);
			if(requestLimit < 0)
			{
				requestLimit = 0;
			}

			if(requestLimit == 0)
			{
				Console.WriteLine($"Исчерпан лимит ежедневных обращений на токене: {_apiToken}");
				return;
			}
			Console.WriteLine($"Лимит обращений на сегодня: {requestLimit}");

			ProcessDeliveryPoints(_apiToken, requestLimit);
		}

		private void ProcessDeliveryPoints(string token, int limit)
		{
			var api = new SuggestClientAsync(token);

			var deliveryPoints = GetDeliveryPoints(limit);
			int counter = 0;
			var count = deliveryPoints.Count;
			foreach(var deliveryPoint in deliveryPoints)
			{
				Console.Write($"{++counter}/{count} | ");
				ProcessDeliveryPoint(deliveryPoint, api);
			}
		}

		private void ProcessDeliveryPoint(DeliveryPoint deliveryPoint, SuggestClientAsync dadataClient)
		{
			var task = dadataClient.Geolocate(deliveryPoint.Latitude, deliveryPoint.Longitude, 100);
			task.Wait();
			
			var address = task.Result.suggestions.FirstOrDefault();
			if(address == null)
			{
				SetAddressNotFoundToDB(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | street not found");
				return;
			}

			var matchedStreetGuids = task.Result.suggestions
				.Where(x => !string.IsNullOrWhiteSpace(x.data.street_fias_id) && !string.IsNullOrWhiteSpace(x.data.city_fias_id))
				.Select(x => new Guid(x.data.street_fias_id))
				.Distinct();
			var availableStreets = _streetRepository.GetStreets(matchedStreetGuids);

			if(!availableStreets.Any())
			{
				SetAddressNotFoundToDB(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | street not found");
				return;
			}

			var availableStreet = availableStreets.FirstOrDefault(x => deliveryPoint.Street.Contains(x.Name, StringComparison.OrdinalIgnoreCase));
			if(availableStreet == null)
			{
				SetAddressNotFoundToDB(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | street not found");
				return;
			}

			Guid? cityGuid = null;
			if(!string.IsNullOrWhiteSpace(address.data.city_fias_id))
			{
				cityGuid = new Guid($"{address.data.city_fias_id}");
			}

			if(!string.IsNullOrWhiteSpace(address.data?.settlement_fias_id))
			{
				cityGuid = new Guid($"{address.data.settlement_fias_id}");
			}

			if(cityGuid == null)
			{
				SetCityNotFoundToDB(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | city not found");
				return;
			}

			var city = _cityRepository.GetCity(cityGuid.Value);
			if(city == null)
			{
				SetCityNotFoundToDB(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | city not found");
				return;
			}

			var street = availableStreet;

			var oldStreet = deliveryPoint.Street;
			var oldCompiledStreet = deliveryPoint.ShortAddress;

			deliveryPoint.City = city.Name;
			deliveryPoint.CityGuid = city.FiasGuid;
			deliveryPoint.LocalityType = city.TypeName;
			deliveryPoint.LocalityTypeShort = city.TypeShortName;

			deliveryPoint.Street = street.Name;
			deliveryPoint.StreetGuid = street.FiasGuid;
			deliveryPoint.StreetType = street.TypeName;
			deliveryPoint.StreetTypeShort = street.TypeShortName;

			deliveryPoint.StreetDistrict = street.StreetDistrict;
			deliveryPoint.StreetTerritory = street.StreetTerritory;

			Exception exception = null;
			//attempt 1
			try
			{
				SaveDeliveryPoint(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | [{oldStreet}] -> [{deliveryPoint.StreetType}, {deliveryPoint.Street}] | [{oldCompiledStreet}] -> [{deliveryPoint.ShortAddress}]");
				return;
			}
			catch(Exception ex)
			{
				exception = ex;
			}

			//attempt 2
			try
			{
				SaveDeliveryPoint(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | [{oldStreet}] -> [{deliveryPoint.StreetType}, {deliveryPoint.Street}] | [{oldCompiledStreet}] -> [{deliveryPoint.ShortAddress}]");
				return;
			}
			catch(Exception ex)
			{
			}

			//attempt 3
			try
			{
				SaveDeliveryPoint(deliveryPoint);
				Console.WriteLine($"Id {deliveryPoint.Id} | [{oldStreet}] -> [{deliveryPoint.StreetType}, {deliveryPoint.Street}] | [{oldCompiledStreet}] -> [{deliveryPoint.ShortAddress}]");
				return;
			}
			catch(Exception ex)
			{
			}

			Console.WriteLine($"Ошибка сохранения в ДВ. Id {deliveryPoint.Id}. Exception: {exception.Message}");
		}

		private int GetRequestsStatistic(string token, string secret)
		{
			var api = new ProfileClientAsync(_apiToken, _secretToken);
			var task = api.GetDailyStats();
			task.Wait();
			var result = task.Result.services.suggestions;
			return result;
		}

		private void SaveDeliveryPoint(DeliveryPoint deliveryPoint)
		{
			using(var vodovozSession = _vodovozSessionFactory.OpenSession())
			using(var transaction = vodovozSession.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var updateSql = $@"UPDATE delivery_points dp
SET	
	dp.city = :city_param,
	dp.locality_type = :locality_type_param,
	dp.locality_type_short = :locality_type_short_param,
	dp.street = :street_param,
	dp.street_type = :street_type_param,
	dp.street_type_short = :street_type_short_param,
	dp.street_district = :street_district_param,
	dp.street_territory = :street_territory_param,
	dp.compiled_address = :comp_address_param,
	dp.compiled_address_short = :comp_address_short_param,
	dp.city_fias_guid = :city_guid_param,
	dp.street_fias_guid = :street_guid_param
WHERE 
	dp.id = :id_param
LIMIT 1
";
				var query = vodovozSession.CreateSQLQuery(updateSql);
				query.SetParameter("city_param", deliveryPoint.City);
				query.SetParameter("locality_type_param", deliveryPoint.LocalityType);
				query.SetParameter("locality_type_short_param", deliveryPoint.LocalityTypeShort);
				query.SetParameter("street_param", deliveryPoint.Street);
				query.SetParameter("street_type_param", deliveryPoint.StreetType);
				query.SetParameter("street_type_short_param", deliveryPoint.StreetTypeShort);
				query.SetParameter("street_district_param", deliveryPoint.StreetDistrict);
				query.SetParameter("street_territory_param", deliveryPoint.StreetTerritory);
				query.SetParameter("comp_address_param", deliveryPoint.CompiledAddress);
				query.SetParameter("comp_address_short_param", deliveryPoint.ShortAddress);
				query.SetParameter("city_guid_param", deliveryPoint.CityGuid);
				query.SetParameter("street_guid_param", deliveryPoint.StreetGuid);
				query.SetParameter("id_param", deliveryPoint.Id);

				var rowCount = query.ExecuteUpdate();
				if(rowCount > 1)
				{
					throw new InvalidOperationException("Обновляется больше строк чем необходимо!");
				}
				transaction.Commit();
			}
		}

		private void SetAddressNotFoundToDB(DeliveryPoint deliveryPoint)
		{
			using(var vodovozSession = _vodovozSessionFactory.OpenSession())
			using(var transaction = vodovozSession.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var updateSql = $@"UPDATE delivery_points dp
SET
	dp.street_territory = 'street_not_found'
WHERE 
	dp.id = :id_param
LIMIT 1
";
				var query = vodovozSession.CreateSQLQuery(updateSql);
				query.SetParameter("id_param", deliveryPoint.Id);

				var rowCount = query.ExecuteUpdate();
				if(rowCount > 1)
				{
					throw new InvalidOperationException("Обновляется больше строк чем необходимо!");
				}
				transaction.Commit();
			}
		}

		private void SetHouseNotFoundToDB(DeliveryPoint deliveryPoint)
		{
			using(var vodovozSession = _vodovozSessionFactory.OpenSession())
			using(var transaction = vodovozSession.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var updateSql = $@"UPDATE delivery_points dp
SET
	dp.street_territory = 'house_not_found'
WHERE 
	dp.id = :id_param
LIMIT 1
";
				var query = vodovozSession.CreateSQLQuery(updateSql);
				query.SetParameter("id_param", deliveryPoint.Id);

				var rowCount = query.ExecuteUpdate();
				if(rowCount > 1)
				{
					throw new InvalidOperationException("Обновляется больше строк чем необходимо!");
				}
				transaction.Commit();
			}
		}

		private void SetCityNotFoundToDB(DeliveryPoint deliveryPoint)
		{
			using(var vodovozSession = _vodovozSessionFactory.OpenSession())
			using(var transaction = vodovozSession.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var updateSql = $@"UPDATE delivery_points dp
SET
	dp.street_territory = 'city_not_found'
WHERE 
	dp.id = :id_param
LIMIT 1
";
				var query = vodovozSession.CreateSQLQuery(updateSql);
				query.SetParameter("id_param", deliveryPoint.Id);

				var rowCount = query.ExecuteUpdate();
				if(rowCount > 1)
				{
					throw new InvalidOperationException("Обновляется больше строк чем необходимо!");
				}
				transaction.Commit();
			}
		}

		private IList<DeliveryPoint> GetDeliveryPoints(int limit)
		{
			using(var vodovozSession = _vodovozSessionFactory.OpenSession())
			{
				var sql = $@"SELECT
	dp.id AS {nameof(DeliveryPoint.Id)}, 
	dp.city AS {nameof(DeliveryPoint.City)},  
	dp.locality_type AS {nameof(DeliveryPoint.LocalityType)}, 
	dp.locality_type_short AS {nameof(DeliveryPoint.LocalityTypeShort)}, 
	dp.street AS {nameof(DeliveryPoint.Street)}, 
	dp.street_type AS {nameof(DeliveryPoint.StreetType)}, 
	dp.street_type_short AS {nameof(DeliveryPoint.StreetTypeShort)}, 
	dp.street_district AS {nameof(DeliveryPoint.StreetDistrict)}, 
	dp.street_territory AS {nameof(DeliveryPoint.StreetTerritory)}, 
	dp.building AS {nameof(DeliveryPoint.Building)}, 
	dp.letter AS {nameof(DeliveryPoint.Letter)}, 
	dp.entrance_type AS {nameof(DeliveryPoint.EntranceType)}, 
	dp.entrance AS {nameof(DeliveryPoint.Entrance)}, 
	dp.floor AS {nameof(DeliveryPoint.Floor)}, 
	dp.room_type AS {nameof(DeliveryPoint.RoomType)}, 
	dp.room AS {nameof(DeliveryPoint.Room)}, 
	dp.address_addition AS {nameof(DeliveryPoint.АddressAddition)}, 
	dp.latitude AS {nameof(DeliveryPoint.Latitude)}, 
	dp.longitude AS {nameof(DeliveryPoint.Longitude)}
FROM 
	delivery_points dp 
WHERE 
	(dp.street_fias_guid IS NULL OR dp.city_fias_guid IS NULL)
	AND
	(dp.latitude IS NOT NULL AND dp.longitude IS NOT NULL)
	AND (dp.street_territory NOT IN ('street_not_found' , 'house_not_found', 'city_not_found') OR dp.street_territory IS NULL)
	AND dp.id >= {_idFrom} AND dp.id <= {_idTo}
LIMIT {limit}
";
				var query = vodovozSession.CreateSQLQuery(sql);

				query.AddScalar(nameof(DeliveryPoint.Id), NHibernateUtil.Int32);
				query.AddScalar(nameof(DeliveryPoint.City), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.LocalityType), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.LocalityTypeShort), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.Street), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.StreetType), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.StreetTypeShort), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.StreetDistrict), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.StreetTerritory), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.Building), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.Letter), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.EntranceType), new EntranceTypeStringType());
				query.AddScalar(nameof(DeliveryPoint.Entrance), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.Floor), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.RoomType), new RoomTypeStringType());
				query.AddScalar(nameof(DeliveryPoint.Room), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.АddressAddition), NHibernateUtil.String);
				query.AddScalar(nameof(DeliveryPoint.Latitude), NHibernateUtil.Double);
				query.AddScalar(nameof(DeliveryPoint.Longitude), NHibernateUtil.Double);
				query.SetResultTransformer(Transformers.AliasToBean<DeliveryPoint>());
				
				var result = query.List<DeliveryPoint>();
				return result;
			}
		}
	}

	public class DeliveryPoint
	{
		public int Id { get; set; }

		public string LocalityTypeShort { get; set; }
		public string LocalityType { get; set; }
		public string City { get; set; }
		public Guid? CityGuid { get; set; }

		public string StreetType { get; set; }
		public string StreetTypeShort { get; set; }
		public string Street { get; set; }
		public Guid? StreetGuid { get; set; }

		public string StreetDistrict { get; set; }
		public string StreetTerritory { get; set; }

		public string Building { get; set; }
		public string Letter { get; set; }

		public EntranceType EntranceType { get; set; }
		public string Entrance { get; set; }
		public string Floor { get; set; }

		public virtual RoomType RoomType { get; set; }
		public string Room { get; set; }

		public string АddressAddition { get; set; }

		public double Latitude { get; set; }
		public double Longitude { get; set; }



		public virtual string CompiledAddress
		{
			get
			{
				string address = string.Empty;
				if(!string.IsNullOrWhiteSpace(City))
					address += $"{LocalityTypeShort}. {City}, ";
				if(!string.IsNullOrWhiteSpace(StreetType))
					address += $"{StreetType.ToLower()} ";
				if(!string.IsNullOrWhiteSpace(Street))
					address += $"{Street}, ";
				if(!string.IsNullOrWhiteSpace(Building))
					address += $"д.{Building}, ";
				if(!string.IsNullOrWhiteSpace(Letter))
					address += $"лит.{Letter}, ";
				if(!string.IsNullOrWhiteSpace(Entrance))
					address += $"{GetEnumShortTitle(EntranceType)} {Entrance}, ";
				if(!string.IsNullOrWhiteSpace(Floor))
					address += $"эт.{Floor}, ";
				if(!string.IsNullOrWhiteSpace(Room))
					address += $"{GetEnumShortTitle(RoomType)} {Room}, ";
				if(!string.IsNullOrWhiteSpace(АddressAddition))
					address += $"{АddressAddition}, ";

				return address.TrimEnd(',', ' ');
			}
		}

		public virtual string ShortAddress
		{
			get
			{
				string address = string.Empty;
				if(!string.IsNullOrWhiteSpace(City) && City != "Санкт-Петербург")
					address += $"{LocalityTypeShort}. {City}, ";
				if(!string.IsNullOrWhiteSpace(StreetTypeShort))
					address += $"{StreetTypeShort}. ";
				if(!string.IsNullOrWhiteSpace(Street))
					address += $"{Street}, ";
				if(!string.IsNullOrWhiteSpace(Building))
					address += $"д.{Building}, ";
				if(!string.IsNullOrWhiteSpace(Letter))
					address += $"лит.{Letter}, ";
				if(!string.IsNullOrWhiteSpace(Entrance))
					address += $"{GetEnumShortTitle(EntranceType)} {Entrance}, ";
				if(!string.IsNullOrWhiteSpace(Floor))
					address += $"эт.{Floor}, ";
				if(!string.IsNullOrWhiteSpace(Room))
					address += $"{GetEnumShortTitle(RoomType)} {Room}, ";

				return address.TrimEnd(',', ' ');
			}
		}

		private string GetEnumShortTitle(Enum aEnum)
		{
			string desc = aEnum.ToString();
			FieldInfo fi = aEnum.GetType().GetField(desc);
			return (GetShortTitle(fi));
		}

		private string GetShortTitle(FieldInfo aFieldInfo)
		{
			var attrs = (DisplayAttribute[])aFieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false);
			if((attrs != null) && (attrs.Length > 0))
			{
				string shortname = attrs[0].GetShortName();
				if(String.IsNullOrWhiteSpace(shortname))
					shortname = attrs[0].GetName();
				return (shortname);
			}
			return (aFieldInfo.Name);
		}
	}

	public enum EntranceType
	{
		[Display(Name = "Парадная", ShortName = "пар.")]
		Entrance,
		[Display(Name = "Торговый центр", ShortName = "ТЦ")]
		TradeCenter,
		[Display(Name = "Торговый комплекс", ShortName = "ТК")]
		TradeComplex,
		[Display(Name = "Бизнесс центр", ShortName = "БЦ")]
		BusinessCenter,
		[Display(Name = "Школа", ShortName = "шк.")]
		School,
		[Display(Name = "Общежитие", ShortName = "общ.")]
		Hostel
	}

	public class EntranceTypeStringType : NHibernate.Type.EnumStringType
	{
		public EntranceTypeStringType() : base(typeof(EntranceType)) { }
	}

	public enum RoomType
	{
		[Display(Name = "Квартира", ShortName = "кв.")]
		Apartment,
		[Display(Name = "Офис", ShortName = "оф.")]
		Office,
		[Display(Name = "Склад", ShortName = "склад")]
		Store,
		[Display(Name = "Помещение", ShortName = "пом.")]
		Room,
		[Display(Name = "Комната", ShortName = "ком.")]
		Chamber,
		[Display(Name = "Секция", ShortName = "сек.")]
		Section
	}

	public class RoomTypeStringType : NHibernate.Type.EnumStringType
	{
		public RoomTypeStringType() : base(typeof(RoomType))
		{
		}
	}

	public class DadataToken
	{
		public DadataToken(string apiToken, string secretToken)
		{
			ApiToken = apiToken;
			SecretToken = secretToken;
		}

		public string ApiToken { get; set; }
		public string SecretToken { get; set; }
		public string Proxy { get; set; }
		public HttpClient httpClient { get; set; }
	}
}
