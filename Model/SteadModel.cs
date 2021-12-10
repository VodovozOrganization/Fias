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
	public class SteadModel
	{
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 1000;


		public SteadModel(FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateSteads(int regionCode)
		{
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasStead>(regionCode))
			{
				int processedCounter = 0;
				List<FiasStead> batch = new List<FiasStead>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasStead = fiasReader.ReadNext();
					batch.Add(fiasStead);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessFiasSteads(batch);
						processedCounter += batch.Count;
						Console.Write($"\rЗагрузка земельных участков. Регион {regionCode}. Загружено {processedCounter}");
						batch = new List<FiasStead>();
						loadCount = 0;
					}
				}
				ProcessFiasSteads(batch);
				processedCounter += batch.Count;
				Console.WriteLine($"\rЗагрузка земельных участков. Регион {regionCode}. Загружено {processedCounter}");
			}
		}

		private void ProcessFiasSteads(IList<FiasStead> fiasSteads)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var existingSteads = GetExistingSteads(fiasSteads);

				foreach(var fiasStead in fiasSteads)
				{
					ProcessFiasStead(session, fiasStead, existingSteads);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessFiasStead(ISession session, FiasStead fiasStead, IList<Stead> existedSteads)
		{
			var stead = existedSteads.FirstOrDefault(x => x.FiasSteadId == fiasStead.Id);
			if(stead == null)
			{
				stead = new Stead();
			}
			UpdateStead(stead, fiasStead);
			session.SaveOrUpdate(stead);
		}

		private void UpdateStead(Stead stead, FiasStead fiasStead)
		{
			stead.FiasSteadId = fiasStead.Id;
			stead.FiasSteadGuid = new Guid(fiasStead.ObjectGuid);
			stead.Number = fiasStead.Number;
			stead.PreviousId = fiasStead.PreviousId;
			stead.NextId = fiasStead.NextId;
			stead.UpdateDate = fiasStead.UpdateDate;
			stead.StartDate = fiasStead.StartDate;
			stead.EndDate = fiasStead.EndDate;
			stead.IsActive = fiasStead.IsActive;
			stead.IsActual = fiasStead.IsActual;
		}

		private IList<Stead> GetExistingSteads(IList<FiasStead> fiasSteads)
		{
			var steadFiasIds = fiasSteads
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var steads = session.QueryOver<Stead>()
					.WhereRestrictionOn(x => x.FiasSteadId).IsIn(steadFiasIds)
					.List();
				return steads;
			}
		}
	}
}
