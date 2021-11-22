using Fias.Search.DTO;
using NHibernate;
using NHibernate.Transform;
using System;
using System.Collections.Generic;

namespace Fias.Search
{
	public class ApartmentRepository
	{
		private readonly ISessionFactory _sessionFactory;

		public ApartmentRepository(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public IEnumerable<ApartmentDTO> GetApartments(Guid houseGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			ahh.fias_house_guid = '{houseGuid}'
			AND a.is_active = {isActive}";

				var query = GetQuery(where, limit);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(ApartmentDTO)))
					.List<ApartmentDTO>();
				return result;
			}
		}

		public IEnumerable<ApartmentDTO> GetApartments(string apartmentNumberSubstring, Guid houseGuid, int? limit = null, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			ahh.fias_house_guid = '{houseGuid}'
			AND a.""number"" ILIKE '{apartmentNumberSubstring}%'
			AND a.is_active = {isActive}";

				var query = GetQuery(where, limit);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(ApartmentDTO)))
					.List<ApartmentDTO>();
				return result;
			}
		}

		public ApartmentDTO GetApartment(Guid apartmentGuid, bool isActive = true)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var where = $@"WHERE
			a.fias_apartment_guid = '{apartmentGuid}'
			AND a.is_active = {isActive}";
				var query = GetQuery(where);

				var result = session.CreateSQLQuery(query)
					.SetResultTransformer(Transformers.AliasToBean(typeof(ApartmentDTO)))
					.UniqueResult<ApartmentDTO>();
				return result;
			}
		}

		private string GetQuery(string where, int? limit = null)
		{
			var limitQuery = limit == null ? "" : $"\nLIMIT {limit}";
			var query = $@"
SELECT
	a.id AS {nameof(ApartmentDTO.Id)},
	a.fias_apartment_guid AS {nameof(ApartmentDTO.FiasGuid)},
	a.""number"" AS {nameof(ApartmentDTO.Number)},
	at1.""name"" AS {nameof(ApartmentDTO.TypeName)},
	at1.short_name AS {nameof(ApartmentDTO.TypeShortName)},
	at1.description AS {nameof(ApartmentDTO.TypeDescription)}
FROM
	apartments a
	INNER JOIN apartment_house_hierarchy ahh ON ahh.fias_apartment_guid = a.fias_apartment_guid
	LEFT JOIN apartment_types at1 ON at1.id = a.apartment_type
{where}{limitQuery}";
			return query;
		}
	}
}
