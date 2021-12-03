using Fias.Search.DTO;
using NHibernate;
using NHibernate.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fias.Search
{
	public class HouseRepository
	{
		private readonly ISessionFactory _sessionFactory;

		public HouseRepository(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public IEnumerable<HouseDTO> GetHousesFromStreet(string houseNumberSubstring, Guid streetGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var houseNumbers = GetHouseNumbers(houseNumberSubstring);
				if(!houseNumbers.Any())
				{
					return Enumerable.Empty<HouseDTO>();
				}
				int houseNumber = houseNumbers[0];
				int? corpusNumber = null;
				int? buildingNumber = null;

				if(houseNumbers.Count > 1)
				{
					corpusNumber = houseNumbers[1];
				}

				if(houseNumbers.Count > 2)
				{
					buildingNumber = houseNumbers[2];
				}

				var query = GetQuery(houseNumber, corpusNumber, buildingNumber, streetGuid, true, limit, isActive);

				var sqlQuery = session.CreateSQLQuery(query);
				SetResultTypes(sqlQuery);
				var result = sqlQuery.List<HouseDTO>();
				return result;
			}
		}

		public IEnumerable<HouseDTO> GetHousesFromCity(string houseNumberSubstring, Guid cityGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var houseNumbers = GetHouseNumbers(houseNumberSubstring);
				if(!houseNumbers.Any())
				{
					return Enumerable.Empty<HouseDTO>();
				}
				int houseNumber = houseNumbers[0];
				int? corpusNumber = null;
				int? buildingNumber = null;

				if(houseNumbers.Count > 1)
				{
					corpusNumber = houseNumbers[1];
				}

				if(houseNumbers.Count > 2)
				{
					buildingNumber = houseNumbers[2];
				}

				var query = GetQuery(houseNumber, corpusNumber, buildingNumber, cityGuid, false, limit, isActive);

				var sqlQuery = session.CreateSQLQuery(query);
				SetResultTypes(sqlQuery);
				var result = sqlQuery.List<HouseDTO>();
				return result;
			}
		}

		private IList<int> GetHouseNumbers(string houseSubstring)
		{
			Regex rgx = new Regex(@"\d{1,}");
			var matches = rgx.Matches(houseSubstring);
			List<int> result = new List<int>();

			foreach(Match match in matches)
			{
				if(!int.TryParse(match.Value, out int houseNumber))
				{
					continue;
				}
				result.Add(houseNumber);
			}
			return result;
		}

		private string GetQuery(int houseNumber, int? corpusNumber, int? buildingNumber, Guid parentGuid, bool isStreet, int? limit = null, bool isActive = true)
		{
			var limitQuery = limit == null ? "" : $"\nLIMIT {limit}";
			var cityHouseQuery = isStreet ? "FALSE" : $"hch.fias_city_guid = '{parentGuid}'";
			var citySteadQuery = isStreet ? "FALSE" : $"sch.fias_city_guid = '{parentGuid}'";
			var streetHouseQuery = isStreet ? $"hsh.fias_street_guid = '{parentGuid}'" : "FALSE";
			var streetSteadQuery = isStreet ? $"ssh.fias_street_guid = '{parentGuid}'" : "FALSE";
			var houseQuery = $"'{houseNumber}%'";
			var corpusQuery = corpusNumber != null ? $"'{corpusNumber.Value}%'" : "''";
			var buildingQuery = buildingNumber != null ? $"'{buildingNumber.Value}%'" : "''";
			var query = $@"
SELECT
	house.id AS {nameof(HouseDTO.Id)},
	house.object_type AS {nameof(HouseDTO.HouseObjectType)},
	house.fias_guid AS {nameof(HouseDTO.FiasGuid)},
	ht1.""name"" AS {nameof(HouseDTO.HouseTypeName)},
	ht1.short_name AS {nameof(HouseDTO.HouseTypeShortName)},
	ht1.description AS {nameof(HouseDTO.HouseTypeDescription)},
	house.number1 AS {nameof(HouseDTO.ObjectNumber)},
	ht2.""name"" AS {nameof(HouseDTO.AddType1Name)},
	ht2.short_name AS {nameof(HouseDTO.AddType1ShortName)},
	ht2.description AS {nameof(HouseDTO.AddType1Description)},
	house.number2 AS {nameof(HouseDTO.AddNumber1)},
	ht3.""name"" AS {nameof(HouseDTO.AddType2Name)},
	ht3.short_name AS {nameof(HouseDTO.AddType2ShortName)},
	ht3.description AS {nameof(HouseDTO.AddType2Description)},
	house.number3 AS {nameof(HouseDTO.AddNumber2)},
	hc.lat AS {nameof(HouseDTO.Latitude)},
	hc.lon AS {nameof(HouseDTO.Longitude)}
FROM
(
	SELECT
		house_inn.id,
		house_inn.object_type,
		house_inn.fias_guid,
		house_inn.number1,
		--Меняем владение на корпус, домовладение на строение
		CASE
			WHEN(house_inn.type1 = 1) THEN 10
			WHEN(house_inn.type1 = 3) THEN 7
			WHEN(house_inn.type1 = 4) THEN 9
			ELSE house_inn.type1
		END AS type1,
		house_inn.number2,
		--Меняем владение на корпус, домовладение на строение
		CASE
			WHEN(house_inn.type2 = 1) THEN 10
			WHEN(house_inn.type2 = 3) THEN 7
			WHEN(house_inn.type2 = 2) THEN 7
			WHEN(house_inn.type2 = 4) THEN 9
			ELSE house_inn.type2
		END AS type2,
		house_inn.number3,
		--Меняем владение на корпус, домовладение на строение
		CASE
			WHEN(house_inn.type3 = 1) THEN 10
			WHEN(house_inn.type3 = 3) THEN 7
			WHEN(house_inn.type3 = 2) THEN 7
			WHEN(house_inn.type3 = 4) THEN 9
			ELSE house_inn.type3
		END AS type3
	FROM
	(
		SELECT
			h.id,
			'House' AS object_type,
			h.fias_house_guid AS fias_guid,
			--Номера корпусов почему - то записано в основном номере а номера домов в первом добавочном, поэтому
			--Меняем номера местами(основной со вторым добавочным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.add_number_1
				ELSE h.""number""
			END AS number1,
			--Меняем типы местами(основной со вторым добавочным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.add_type_1
				ELSE h.house_type
			END AS type1,
			--Меняем номера местами(второй добавочный с основным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.""number""
				ELSE h.add_number_1
			END AS number2,
			--Меняем типы местами(второй добавочный с основным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.house_type
				ELSE h.add_type_1
			END AS type2,
			h.add_number_2 AS number3,
			h.add_type_2 AS type3
		FROM
			houses h
			INNER JOIN house_city_hierarchy hch ON hch.fias_house_guid = h.fias_house_guid
		WHERE
			{cityHouseQuery}
			AND h.is_active = {isActive}
			--Исключаем не нужные типы помещений
			AND(h.house_type NOT IN(6, 11, 12, 13, 14) OR h.house_type IS NULL)
			AND(h.add_type_1 NOT IN(6, 11, 12, 13, 14) OR h.add_type_1 IS NULL)
			AND(h.add_type_2 NOT IN(6, 11, 12, 13, 14) OR h.add_type_2 IS NULL)
			--Поиск дома
			AND 
			(
				(
					(h.""number"" ILIKE {houseQuery} AND (h.add_number_1 ILIKE {corpusQuery} OR {corpusQuery} = '') AND h.house_type != 10)
					OR
					--Если тип дома корпус, то надо поменять их с домом местами
					(h.""number"" ILIKE {corpusQuery} AND(h.add_number_1 ILIKE {houseQuery}) AND h.house_type = 10)
				)
				AND
				(h.add_number_2 ILIKE {buildingQuery} OR ({buildingQuery} = ''))
			)
		UNION ALL
		SELECT
			h.id,
			'House' AS object_type,
			h.fias_house_guid AS fias_guid,
			--Номера корпусов почему - то записаны в основном номере а номера домов в первом добавочном, поэтому
			--Меняем номера местами(основной со вторым добавочным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.add_number_1
				ELSE h.""number""
			END AS number1,
			--Меняем типы местами(основной со вторым добавочным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.add_type_1
				ELSE h.house_type
			END AS type1,
			--Меняем номера местами(второй добавочный с основным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.""number""
				ELSE h.add_number_1
			END AS number2,
			--Меняем типы местами(второй добавочный с основным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.house_type
				ELSE h.add_type_1
			END AS type2,
			h.add_number_2 AS number3,
			h.add_type_2 AS type3
		FROM
			houses h
			INNER JOIN house_street_hierarchy hsh ON hsh.fias_house_guid = h.fias_house_guid
		WHERE
			{streetHouseQuery}
			AND h.is_active = {isActive}
			--Исключаем не нужные типы помещений
			AND(h.house_type NOT IN(6, 11, 12, 13, 14) OR h.house_type IS NULL)
			AND(h.add_type_1 NOT IN(6, 11, 12, 13, 14) OR h.add_type_1 IS NULL)
			AND(h.add_type_2 NOT IN(6, 11, 12, 13, 14) OR h.add_type_2 IS NULL)
			--Поиск дома
			AND 
			(
				(
					(h.""number"" ILIKE {houseQuery} AND (h.add_number_1 ILIKE {corpusQuery} OR {corpusQuery} = '') AND h.house_type != 10)
					OR
					--Если тип дома корпус, то надо поменять их с домом местами
					(h.""number"" ILIKE {corpusQuery} AND(h.add_number_1 ILIKE {houseQuery}) AND h.house_type = 10)
				)
				AND
				(h.add_number_2 ILIKE {buildingQuery} OR ({buildingQuery} = ''))
			)
		UNION ALL
		SELECT
			s.id,
			'Stead' AS object_type,
			s.fias_stead_guid AS fias_guid,
			s.""number"" AS number1,
			--Представляем земельный участок как дом
			2 AS type1,
			NULL AS number2,
			NULL AS type2,
			NULL AS number3,
			NULL AS type3
		FROM
			steads s
			INNER JOIN stead_city_hierarchy sch ON sch.fias_stead_guid = s.fias_stead_guid
		WHERE
			{citySteadQuery}
			AND s.is_active = {isActive}
			AND s.""number"" ILIKE {houseQuery}
		UNION ALL
		SELECT
			s.id,
			'Stead' AS object_type,
			s.fias_stead_guid AS fias_guid,
			s.""number"" AS number1,
			--Представляем земельный участок как дом
			2 AS type1,
			NULL AS number2,
			NULL AS type2,
			NULL AS number3,
			NULL AS type3
		FROM
			steads s
			INNER JOIN stead_street_hierarchy ssh ON ssh.fias_stead_guid = s.fias_stead_guid
		WHERE
			{streetSteadQuery}
			AND s.is_active = {isActive}
			AND s.""number"" ILIKE {houseQuery}{limitQuery}
	) house_inn
) house
LEFT JOIN house_types ht1 ON ht1.id = house.type1
LEFT JOIN house_types ht2 ON ht2.id = house.type2
LEFT JOIN house_types ht3 ON ht3.id = house.type3
LEFT JOIN house_coordinate hc ON hc.house_fias_guid = house.fias_guid
";
			return query;
		}

		private void SetResultTypes(ISQLQuery query)
		{
			query.AddScalar(nameof(HouseDTO.Id), NHibernateUtil.Int32);
			query.AddScalar(nameof(HouseDTO.FiasGuid), NHibernateUtil.Guid);
			query.AddScalar(nameof(HouseDTO.HouseObjectType), new HouseObjectTypeStringType());
			query.AddScalar(nameof(HouseDTO.ObjectNumber), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.HouseTypeName), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.HouseTypeShortName), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.HouseTypeDescription), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddNumber1), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddType1Name), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddType1ShortName), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddType1Description), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddNumber2), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddType2Name), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddType2ShortName), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.AddType2Description), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.Latitude), NHibernateUtil.String);
			query.AddScalar(nameof(HouseDTO.Longitude), NHibernateUtil.String);

			query.SetResultTransformer(Transformers.AliasToBean(typeof(HouseDTO)));
		}
	}
}
