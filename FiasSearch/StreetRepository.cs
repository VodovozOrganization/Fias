using Fias.Search.DTO;
using NHibernate;
using NHibernate.Transform;
using System;
using System.Collections.Generic;

namespace Fias.Search
{
	public class StreetRepository
    {
		private readonly ISessionFactory _sessionFactory;

		public StreetRepository(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public IEnumerable<StreetDTO> GetStreets(Guid cityGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			sch.fias_city_guid = '{cityGuid}'
			AND s.is_active = {isActive}";
				var query = GetQuery(where, limit);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(StreetDTO)))
					.List<StreetDTO>();
				return result;
			}
		}

		public IEnumerable<StreetDTO> GetStreets(string streetNameSubstring, Guid cityGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			sch.fias_city_guid = '{cityGuid}'
			AND s.""name"" ILIKE '%{streetNameSubstring}%'
			AND s.is_active = {isActive}";
				var query = GetQuery(where, limit);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(StreetDTO)))
					.List<StreetDTO>();
				return result;
			}
		}

		public StreetDTO GetStreet(Guid streetGuid, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			s.fias_street_guid = '{streetGuid}'
			AND s.is_active = {isActive}";
				var query = GetQuery(where);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(StreetDTO)))
					.UniqueResult<StreetDTO>();
				return result;
			}
		}

		private string GetQuery(string where, int? limit = null)
		{
			var limitQuery = limit == null ? "" : $"\nLIMIT {limit}";
			var query = $@"
SELECT
	s.id AS {nameof(StreetDTO.Id)},
	s.fias_street_guid AS {nameof(StreetDTO.FiasGuid)},
	s.""name"" AS {nameof(StreetDTO.Name)},
	st.""name"" AS {nameof(StreetDTO.TypeName)},
	st.short_name AS {nameof(StreetDTO.TypeShortName)},
	st.description AS {nameof(StreetDTO.TypeDescription)}
FROM
	public.street_city_hierarchy sch
	INNER JOIN public.streets s ON s.fias_street_guid = sch.fias_street_guid
	LEFT JOIN public.street_types st ON st.id = s.type_id
{where}{limitQuery}
";
			return query;
		}
	}
}
