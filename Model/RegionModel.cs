using Fias.Domain.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Data;

namespace Fias.LoadModel
{
	public class RegionModel
	{
		private readonly ISessionFactory _sessionFactory;

		public RegionModel(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void CreateRegions()
		{
			Console.WriteLine("Создание регионов.");
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var region78 = CreateRegion(78, "Санкт-Петербург");
				var region47 = CreateRegion(47, "Ленинградская область");
				session.SaveOrUpdate(region78);
				session.SaveOrUpdate(region47);
				session.Flush();
				transaction.Commit();
			}
		}

		public IEnumerable<Region> GetRegions()
		{
			using(var session = _sessionFactory.OpenSession())
			{
				return session.QueryOver<Region>().List();
			}
		}

		private Region CreateRegion(int code, string name)
		{
			var region = new Region();
			region.Code = code;
			region.Name = name;
			return region;
		}
	}
}
