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
	public class ReestrObjectModel
	{
		private readonly LevelModel _levelModel;
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 1000;


		public ReestrObjectModel(LevelModel levelModel, FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_levelModel = levelModel ?? throw new ArgumentNullException(nameof(levelModel));
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateReestrObjects(int regionCode)
		{
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasReestrObject>(regionCode))
			{
				List<FiasReestrObject> batch = new List<FiasReestrObject>();
				int processedCounter = 0;
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasReestrObject = fiasReader.ReadNext();
					batch.Add(fiasReestrObject);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessFiasReestrObjects(batch);
						processedCounter += batch.Count;
						Console.Write($"\rЗагрузка реестра объектов для иерархии. Регион {regionCode}. Загружено {processedCounter}");
						batch = new List<FiasReestrObject>();
						loadCount = 0;
					}
				}
				ProcessFiasReestrObjects(batch);
				processedCounter += batch.Count;
				Console.WriteLine($"\rЗагрузка реестра объектов для иерархии. Регион {regionCode}. Загружено {processedCounter}");
			}
		}

		private void ProcessFiasReestrObjects(IList<FiasReestrObject> fiasReestrObjects)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var existedReestrObjects = GetExistedReestrObjects(fiasReestrObjects);

				foreach(var fiasReestrObject in fiasReestrObjects)
				{
					ProcessFiasReestrObject(session, fiasReestrObject, existedReestrObjects);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessFiasReestrObject(ISession session, FiasReestrObject fiasReestrObject, IList<ReestrObject> existedReestrObjects)
		{
			var reestrObject = existedReestrObjects.FirstOrDefault(x => x.Id == fiasReestrObject.Id);
			if(reestrObject == null)
			{
				reestrObject = new ReestrObject();
			}

			UpdateReestrObject(reestrObject, fiasReestrObject);
			session.SaveOrUpdate(reestrObject);
		}

		private void UpdateReestrObject(ReestrObject reestrObject, FiasReestrObject fiasReestrObject)
		{
			reestrObject.Id = fiasReestrObject.Id;
			reestrObject.Level = _levelModel.GetLevel(fiasReestrObject.Level);
			reestrObject.FiasObjectGuid = new Guid(fiasReestrObject.ObjectGuid);
			reestrObject.ChangeId = fiasReestrObject.ChangeId;
			reestrObject.CreateDate = fiasReestrObject.CreateDate;
			reestrObject.UpdateDate = fiasReestrObject.UpdateDate;
			reestrObject.IsActive = fiasReestrObject.IsActive;
		}

		private IList<ReestrObject> GetExistedReestrObjects(IList<FiasReestrObject> fiasReestrObjects)
		{
			var reestrObjectFiasIds = fiasReestrObjects
				.Select(x => x.Id)
				.ToArray();

			using(var session = _sessionFactory.OpenSession())
			{
				var reestrObjects = session.QueryOver<ReestrObject>()
					.WhereRestrictionOn(x => x.Id).IsIn(reestrObjectFiasIds)
					.List();
				return reestrObjects;
			}
		}
	}
}
