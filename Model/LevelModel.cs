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
	public class LevelModel
	{
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;

		private readonly List<ObjectLevel> _levelCache = new List<ObjectLevel>();

		public LevelModel(FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadLevels()
		{
			Console.WriteLine("Загрузка справочника уровней.");
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasObjectLevel>())
			{
				while(fiasReader.CanReadNext)
				{
					var fiasObjectLevel = fiasReader.ReadNext();
					var objectLevel = new ObjectLevel();
					UpdateLevel(objectLevel, fiasObjectLevel);
					session.SaveOrUpdate(objectLevel);
					_levelCache.Add(objectLevel);
				}
				session.Flush();
				transaction.Commit();
			}
		}

		public ObjectLevel GetLevel(int level)
		{
			var objectLevel = _levelCache.FirstOrDefault(x => x.Level == level);
			if(objectLevel == null)
			{
				throw new InvalidOperationException($"Невозможно найти уровень {level}. Возможно уровни не были загружены или передан не правильный уровнь.");
			}
			return objectLevel;
		}

		private void UpdateLevel(ObjectLevel level, FiasObjectLevel fiasLevel)
		{
			level.Level = fiasLevel.Level;
			level.Name = fiasLevel.Name;
			level.ShortName = fiasLevel.ShortName;
			level.StartDate = fiasLevel.StartDate;
			level.UpdateDate = fiasLevel.UpdateDate;
			level.EndDate = fiasLevel.EndDate;
			level.EndDate = fiasLevel.EndDate;
			level.IsActive = fiasLevel.IsActive;
		}
	}
}
