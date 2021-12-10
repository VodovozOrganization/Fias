using NHibernate;
using NHibernate.Transform;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace VodovozStreetsScript
{
	public class CityScript
	{
		private readonly ISessionFactory _fiasSessionFactory;
		private readonly ISessionFactory _vodovozSessionFactory;

		public CityScript(ISessionFactory fiasSessionFactory, ISessionFactory vodovozSessionFactory)
		{
			_fiasSessionFactory = fiasSessionFactory ?? throw new ArgumentNullException(nameof(fiasSessionFactory));
			_vodovozSessionFactory = vodovozSessionFactory ?? throw new ArgumentNullException(nameof(vodovozSessionFactory));
		}

		private Dictionary<string, FiasCityNode> fiasCityDic = new Dictionary<string, FiasCityNode>();
		IList<GoodCity> _goodCities = new List<GoodCity>();
		HashSet<string> duplicates = new HashSet<string>();
		public void Start()
		{
			char[] specialChars = " .,/?!\\|\"'`~<>()&^$;:*%#@[]{}-_+=".ToArray();

			var vodovozCities = GetVodovozCities();
			var fiasCities = LoadFiasCities();
			foreach(var fiasCity in fiasCities.ToList())
			{
				var cityName = fiasCity.CityName.ToLower();
				if(duplicates.Contains(cityName))
				{
					continue;
				}
				if(fiasCityDic.ContainsKey(cityName))
				{
					fiasCityDic.Remove(cityName);
					duplicates.Add(cityName);
				}
				else
				{
					fiasCityDic.Add(cityName, fiasCity);
				}
			}

			//Точное совпадение
			var remainingCities = new List<string>();
			foreach(var vodovozCity in vodovozCities)
			{
				if(fiasCityDic.TryGetValue(vodovozCity.Trim(specialChars).ToLower(), out FiasCityNode node))
				{
					var goodCity = new GoodCity();
					goodCity.VodovozCityName = vodovozCity;
					goodCity.FiasCity = node;
					_goodCities.Add(goodCity);
					fiasCityDic.Remove(vodovozCity.ToLower());
				}
				else
				{
					remainingCities.Add(vodovozCity);
				}
			}

			//Содержание города
			var badCities = new List<string>();
			foreach(var fiasCity in fiasCities)
			{
				var matchingCities = remainingCities.Where(x => x.Contains(fiasCity.CityName, StringComparison.OrdinalIgnoreCase));
				var count = matchingCities.Count();
				if(matchingCities.Any() && fiasCities.Count(x => x.CityName.ToLower() == fiasCity.CityName.ToLower()) > 1)
				{
					foreach(var item in matchingCities)
					{
						badCities.Add(item);
					}
					continue;
				}
				if(count == 1 && !string.IsNullOrWhiteSpace(matchingCities.First()))
				{
					var goodCity = new GoodCity();
					goodCity.VodovozCityName = matchingCities.First();
					goodCity.FiasCity = fiasCity;
					_goodCities.Add(goodCity);
				}
				else
				{
					foreach(var item in matchingCities)
					{
						badCities.Add(item);
					}
				}
			}

			foreach(var savedCity in _goodCities)
			{
				SaveDeliveryPointCity(savedCity.VodovozCityName, savedCity.FiasCity.CityName, savedCity.FiasCity.TypeName, savedCity.FiasCity.TypeShortName, savedCity.FiasCity.CityGuid);
			}

			/*foreach(var item in badCities)
			{
				Console.WriteLine($"{item}");
			}*/
			Console.ReadKey();
		}

		private void SaveDeliveryPointCity(string city, string newName, string type, string typeShort, Guid guid)
		{
			using(var vodovozSession = _vodovozSessionFactory.OpenSession())
			using(var transaction = vodovozSession.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				Console.Write($"Updating '{city} to '{newName}''.");
				Console.Write(" Обработать? (y/n). ");
				var read = Console.ReadLine();
				if(read == "y")
				{
					var sql = $@"UPDATE Vodovoz_honeybee.delivery_points
SET city='{newName}', locality_type='{type}', locality_type_short='{typeShort}', city_fias_guid='{guid}'
WHERE city='{city}' OR city='{newName}'";
					var query = vodovozSession.CreateSQLQuery(sql);
					var rowsUpdated = query.ExecuteUpdate();
					Console.WriteLine($" {rowsUpdated} rows updated.");

					vodovozSession.Flush();
					transaction.Commit();
				}
				
			}
		}

		public IList<string> GetVodovozCities()
		{
			using(var session = _vodovozSessionFactory.OpenSession())
			{
				var result = session.CreateSQLQuery("SELECT DISTINCT city FROM delivery_points dp WHERE dp.city_fias_guid IS NULL AND dp.city IS NOT NULL")
					.List<string>();
				return result;
			}
		}

		public IList<FiasCityNode> LoadFiasCities()
		{
			using(var session = _fiasSessionFactory.OpenSession())
			{
				var result = session.CreateSQLQuery($@"SELECT 
	DISTINCT ON (c.fias_city_guid) 
	c.""name"" as {nameof(FiasCityNode.CityName)}, 
	c.fias_city_guid as {nameof(FiasCityNode.CityGuid)}, 
	ct.""name"" as {nameof(FiasCityNode.TypeName)}, 
	ct.short_name as {nameof(FiasCityNode.TypeShortName)} 
FROM
	cities c
	LEFT JOIN city_types ct ON ct.id = c.type_id
WHERE
	c.is_active")
					.SetResultTransformer(Transformers.AliasToBean<FiasCityNode>())
					.List<FiasCityNode>();
				return result;
			}
		}

		public class GoodCity
		{
			public string VodovozCityName { get; set; }
			public FiasCityNode FiasCity { get; set; }
		}
	}
}
