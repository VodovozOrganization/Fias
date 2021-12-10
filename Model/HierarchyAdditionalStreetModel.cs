using Fias.Domain.Entities;
using Fias.Domain.Entities.Hierarchy;
using Fias.Source;
using Fias.Source.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace Fias.LoadModel
{
	public class HierarchyAdditionalStreetModel
	{
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 1000;


		public HierarchyAdditionalStreetModel(FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateHierarchy(int regionCode)
		{
			LoadMunHierarchy(regionCode);
		}

		private void LoadMunHierarchy(int regionCode)
		{
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasMunHierarchy>(regionCode))
			{
				int processedCounter = 0;
				List<FiasMunHierarchy> batch = new List<FiasMunHierarchy>();
				int loadCount = 0;
				while(fiasReader.CanReadNext)
				{
					var fiasHierarchy = fiasReader.ReadNext();
					batch.Add(fiasHierarchy);
					loadCount++;
					if(loadCount == _batchSize)
					{
						ProcessHierarchyObjects(batch);
						processedCounter += batch.Count;
						Console.Write($"\rЗагрузка Муниципальной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
						batch = new List<FiasMunHierarchy>();
						loadCount = 0;
					}
				}
				ProcessHierarchyObjects(batch);
				Console.WriteLine($"\rЗагрузка Муниципальной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
			}
		}

		private void ProcessHierarchyObjects(IEnumerable<IFiasHierarchy> hierarchyObjects)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var hierarchy = FillHierarchyObjects(hierarchyObjects);
				ProcessStreet(session, hierarchy);
				session.Flush();
				transaction.Commit();
			}
		}

		private HashSet<HierarchyNode> FillHierarchyObjects(IEnumerable<IFiasHierarchy> hierarchyObjects)
		{
			HashSet<HierarchyNode> result = new HashSet<HierarchyNode>();

			var currentObjectIds = hierarchyObjects.Select(x => x.ObjectId).ToArray();
			var parentObjectsIds = hierarchyObjects.Select(x => x.ParentObjectId).ToArray();
			var currentObjects = GetReestrObjects(currentObjectIds);
			var parentObjects = GetReestrObjects(parentObjectsIds);

			foreach(var hierarchyObject in hierarchyObjects)
			{
				if(hierarchyObject.ParentObjectId == 0)
				{
					continue;
				}
				var current = currentObjects.Single(x => x.Id == hierarchyObject.ObjectId);
				var parent = parentObjects.Single(x => x.Id == hierarchyObject.ParentObjectId);
				var node = new HierarchyNode(current.FiasObjectGuid, parent.FiasObjectGuid);
				if(!result.Contains(node))
				{
					result.Add(node);
				}
			}
			return result;
		}

		private IList<ReestrObject> GetReestrObjects(long[] ids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var reestrObjects = session.QueryOver<ReestrObject>()
					.WhereRestrictionOn(x => x.Id).IsIn(ids)
					.List();
				return reestrObjects;
			}
		}

		#region Street

		private void ProcessStreet(ISession session, HashSet<HierarchyNode> hierarchy)
		{
			if(!hierarchy.Any())
			{
				return;
			}

			var currentObjectGuids = hierarchy.Select(x => x.CurrentObjectGuid);
			var parentObjectGuids = hierarchy.Select(x => x.ParentObjectGuid);
			var streetGuids = GetStreetGuids(currentObjectGuids.ToArray());
			var streetParentGuids = GetStreetGuids(parentObjectGuids.ToArray());

			foreach(var hierarchyNode in hierarchy.ToList())
			{
				var streetGuid = hierarchyNode.CurrentObjectGuid;
				var parentGuid = hierarchyNode.ParentObjectGuid;

				if(!streetGuids.Contains(streetGuid))
				{
					continue;
				}

				if(!streetParentGuids.Contains(parentGuid))
				{
					continue;
				}

				var streetHierarchy = new StreetToStreetHierarchy();
				streetHierarchy.FiasStreetGuid = streetGuid;
				streetHierarchy.FiasParentStreetGuid = parentGuid;
				session.SaveOrUpdate(streetHierarchy);
			}
		}

		private HashSet<Guid> GetStreetGuids(Guid[] guids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var cityGuids = session.QueryOver<Street>()
					.WhereRestrictionOn(x => x.FiasStreetGuid).IsIn(guids)
					.Select(Projections.Distinct(Projections.Property<Street>(x => x.FiasStreetGuid)))
					.List<Guid>()
					.ToHashSet();
				return cityGuids;
			}
		}

		#endregion

		private class HierarchyNode
		{
			public Guid CurrentObjectGuid { get; private set; }
			public Guid ParentObjectGuid { get; private set; }

			public HierarchyNode(Guid currentObjectGuid, Guid parentObjectGuid)
			{
				CurrentObjectGuid = currentObjectGuid;
				ParentObjectGuid = parentObjectGuid;
			}

			public override bool Equals(object obj)
			{
				return obj is HierarchyNode node &&
					   CurrentObjectGuid.Equals(node.CurrentObjectGuid) &&
					   ParentObjectGuid.Equals(node.ParentObjectGuid);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(CurrentObjectGuid, ParentObjectGuid);
			}
		}
	}
}
