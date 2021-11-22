using Fias.Search.DTO;
using NHibernate;
using NHibernate.Transform;
using System;
using System.Collections.Generic;

namespace Fias.Search
{
	public class HouseRepository
	{
		private readonly ISessionFactory _sessionFactory;

		public HouseRepository(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public IEnumerable<HouseDTO> GetHousesFromStreet(Guid streetGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var houseHierarchyJoin = "\nINNER JOIN house_street_hierarchy hsh ON hsh.fias_house_guid = h.fias_house_guid";
				var whereHouse = $@"WHERE
			hsh.fias_street_guid = '{streetGuid}'
			AND h.is_active = {isActive}";

				var steadHierarchyJoin = "\nINNER JOIN stead_street_hierarchy ssh ON ssh.fias_stead_guid = s.fias_stead_guid";
				var whereStead = $@"WHERE
			ssh.fias_street_guid = '{streetGuid}'
			AND s.is_active = {isActive}";


				var query = GetQuery(whereHouse, whereStead, houseHierarchyJoin, steadHierarchyJoin, limit);

				var sqlQuery = session.CreateSQLQuery(query);
				SetResultTypes(sqlQuery);
				var result = sqlQuery.List<HouseDTO>();
				return result;
			}
		}

		public IEnumerable<HouseDTO> GetHousesFromCity(Guid cityGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var houseHierarchyJoin = "\nINNER JOIN house_city_hierarchy hch ON hch.fias_house_guid = h.fias_house_guid";
				var whereHouse = $@"WHERE
			hch.fias_city_guid = '{cityGuid}'
			AND h.is_active = {isActive}";

				var steadHierarchyJoin = "\nINNER JOIN stead_city_hierarchy sch ON sch.fias_stead_guid = s.fias_stead_guid";
				var whereStead = $@"WHERE
			sch.fias_city_guid = '{cityGuid}'
			AND s.is_active = {isActive}";


				var query = GetQuery(whereHouse, whereStead, houseHierarchyJoin, steadHierarchyJoin, limit);

				var sqlQuery = session.CreateSQLQuery(query);
				SetResultTypes(sqlQuery);
				var result = sqlQuery.List<HouseDTO>();
				return result;
			}
		}

		public IEnumerable<HouseDTO> GetHousesFromStreet(string houseNumberSubstring, Guid streetGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var houseHierarchyJoin = "\nINNER JOIN house_street_hierarchy hsh ON hsh.fias_house_guid = h.fias_house_guid";
				var whereHouse = $@"WHERE
			hsh.fias_street_guid = '{streetGuid}'
			AND h.""number"" ILIKE '{houseNumberSubstring}%'
			AND h.is_active = {isActive}";

				var steadHierarchyJoin = "\nINNER JOIN stead_street_hierarchy ssh ON ssh.fias_stead_guid = s.fias_stead_guid";
				var whereStead = $@"WHERE
			ssh.fias_street_guid = '{streetGuid}'
			AND s.""number"" ILIKE '{houseNumberSubstring}%'
			AND s.is_active = {isActive}";

				var query = GetQuery(whereHouse, whereStead, houseHierarchyJoin, steadHierarchyJoin, limit);

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
				var houseHierarchyJoin = "\nINNER JOIN house_city_hierarchy hch ON hch.fias_house_guid = h.fias_house_guid";
				var whereHouse = $@"WHERE
			hch.fias_city_guid = '{cityGuid}'
			AND h.""number"" ILIKE '{houseNumberSubstring}%'
			AND h.is_active = {isActive}";

				var steadHierarchyJoin = "\nINNER JOIN stead_city_hierarchy sch ON sch.fias_stead_guid = s.fias_stead_guid";
				var whereStead = $@"WHERE
			sch.fias_city_guid = '{cityGuid}'
			AND s.""number"" ILIKE '{houseNumberSubstring}%'
			AND s.is_active = {isActive}";

				var query = GetQuery(whereHouse, whereStead, houseHierarchyJoin, steadHierarchyJoin, limit);

				var sqlQuery = session.CreateSQLQuery(query);
				SetResultTypes(sqlQuery);
				var result = sqlQuery.List<HouseDTO>();
				return result;
			}
		}

		public HouseDTO GetHouse(Guid houseGuid, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var houseHierarchyJoin = "";
				var whereHouse = $@"WHERE
			h.fias_house_guid = '{houseGuid}'
			AND h.is_active = {isActive}";

				var steadHierarchyJoin = "";
				var whereStead = $@"WHERE
			s.fias_stead_guid = '{houseGuid}'
			AND s.is_active = {isActive}";

				var query = GetQuery(whereHouse, whereStead, houseHierarchyJoin, steadHierarchyJoin);

				var sqlQuery = session.CreateSQLQuery(query);
				SetResultTypes(sqlQuery);				
				var result = sqlQuery.UniqueResult<HouseDTO>();
				return result;
			}
		}

		private string GetQuery(string whereHouse, string whereStead, string houseHierarchyJoin, string steadHierarchyJoin, int? limit = null)
		{
			var limitQuery = limit == null ? "" : $"\nLIMIT {limit}";
			var query = $@"
SELECT
	h.id AS {nameof(HouseDTO.Id)},
	h.fias_house_guid AS {nameof(HouseDTO.FiasGuid)},
	'House' AS {nameof(HouseDTO.HouseObjectType)},
	h.""number"" AS {nameof(HouseDTO.ObjectNumber)},
	ht1.""name"" AS {nameof(HouseDTO.HouseTypeName)},
	ht1.short_name AS {nameof(HouseDTO.HouseTypeShortName)},
	ht1.description AS {nameof(HouseDTO.HouseTypeDescription)},
	h.add_number_1 AS {nameof(HouseDTO.AddNumber1)},
	ht2.""name"" AS {nameof(HouseDTO.AddType1Name)},
	ht2.short_name AS {nameof(HouseDTO.AddType1ShortName)},
	ht2.description AS {nameof(HouseDTO.AddType1Description)},
	h.add_number_2 AS {nameof(HouseDTO.AddNumber2)},
	ht3.""name"" AS {nameof(HouseDTO.AddType2Name)},
	ht3.short_name AS {nameof(HouseDTO.AddType2ShortName)},
	ht3.description AS {nameof(HouseDTO.AddType2Description)},
	null AS {nameof(HouseDTO.Latitude)},
	null AS {nameof(HouseDTO.Longitude)}
FROM
	houses h {houseHierarchyJoin}	
	LEFT JOIN house_types ht1 ON ht1.id = h.house_type
	LEFT JOIN house_types ht2 ON ht2.id = h.add_type_1
	LEFT JOIN house_types ht3 ON ht3.id = h.add_type_2
{whereHouse}
UNION ALL
SELECT
	s.id AS {nameof(HouseDTO.Id)},
	s.fias_stead_guid AS {nameof(HouseDTO.FiasGuid)},
	'Stead' AS {nameof(HouseDTO.HouseObjectType)},
	s.""number"" AS {nameof(HouseDTO.ObjectNumber)},
	null AS {nameof(HouseDTO.HouseTypeName)},
	null AS {nameof(HouseDTO.HouseTypeShortName)},
	null AS {nameof(HouseDTO.HouseTypeDescription)},
	null AS {nameof(HouseDTO.AddNumber1)},
	null AS {nameof(HouseDTO.AddType1Name)},
	null AS {nameof(HouseDTO.AddType1ShortName)},
	null AS {nameof(HouseDTO.AddType1Description)},
	null AS {nameof(HouseDTO.AddNumber2)},
	null AS {nameof(HouseDTO.AddType2Name)},
	null AS {nameof(HouseDTO.AddType2ShortName)},
	null AS {nameof(HouseDTO.AddType2Description)},
	null AS {nameof(HouseDTO.Latitude)},
	null AS {nameof(HouseDTO.Longitude)}
FROM
	steads s {steadHierarchyJoin}	
{whereStead}{limitQuery}";
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
