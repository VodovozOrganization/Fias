using Fias.Search.DTO;
using NHibernate;
using NHibernate.Transform;
using System;
using System.Collections.Generic;

namespace Fias.Search
{
	public class CityRepository
    {
		private readonly ISessionFactory _sessionFactory;

		public CityRepository(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public IEnumerable<CityDTO> GetCities(int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			c.is_active = {isActive}";
				var query = GetQuery(where, limit);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(CityDTO)))
					.List<CityDTO>();
				return result;
			}
		}

		public IEnumerable<CityDTO> GetCities(string cityNameSubstring, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			c.""name"" ILIKE '%{cityNameSubstring}%'
			AND c.is_active = {isActive}";
				var query = GetQuery(where, limit);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(CityDTO)))
					.List<CityDTO>();
				return result;
			}
		}

		public CityDTO GetCity(Guid cityGuid, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			c.fias_city_guid = '{cityGuid}'
			AND c.is_active = {isActive}";
				var query = GetQuery(where);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(CityDTO)))
					.UniqueResult<CityDTO>();
				return result;
			}
		}

		private string GetQuery(string where, int? limit = null)
		{
			var limitQuery = limit == null ? "" : $"\nLIMIT {limit}";
			var query = $@"
SELECT
	c.id AS {nameof(CityDTO.Id)},
	c.region_code AS {nameof(CityDTO.RegionCode)},
	r.""name"" AS {nameof(CityDTO.RegionName)},
	c.fias_city_guid AS {nameof(CityDTO.FiasGuid)},
	c.""name"" AS {nameof(CityDTO.Name)},
	ct.""name"" AS {nameof(CityDTO.TypeName)},
	ct.short_name AS {nameof(CityDTO.TypeShortName)}
FROM
	public.cities c
	LEFT JOIN public.regions r ON r.code = c.region_code
	LEFT JOIN public.city_types ct ON ct.id = c.type_id
{where}{limitQuery}
";
			return query;
		}
	}
}
