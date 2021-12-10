using Fias.Domain.Entities;
using Fias.Domain.Entities.Hierarchy;
using Fias.Source;
using Fias.Source.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Fias.LoadModel
{
	public class HierarchyDistrictModel
	{
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 1000;


		public HierarchyDistrictModel(FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateHierarchy(int regionCode)
		{
			LoadMunDistricts();
			//LoadMunHierarchy(regionCode);
			LoadMunHierarchyIntermediateLevels(regionCode, false);
			LoadMunHierarchyIntermediateLevels(regionCode, true);
		}

		private void LoadMunHierarchy(int regionCode)
		{
			LoadMunDistricts();

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
						Console.Write($"\rЗагрузка муниципальной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
						batch = new List<FiasMunHierarchy>();
						loadCount = 0;
					}
				}
				ProcessHierarchyObjects(batch);
				Console.WriteLine($"\rЗагрузка муниципальной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
			}
		}

		private void LoadMunHierarchyIntermediateLevels(int regionCode, bool secondIter)
		{
			//LoadMunDistricts();

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
						ProcessHierarchyIntermediateObjects(batch, secondIter);
						processedCounter += batch.Count;
						Console.Write($"\rЗагрузка промежуточной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
						batch = new List<FiasMunHierarchy>();
						loadCount = 0;
					}
				}
				ProcessHierarchyIntermediateObjects(batch, secondIter);
				Console.WriteLine($"\rЗагрузка промежуточной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
			}
		}

		Dictionary<Guid, MunDistrict> _munDistricts = new Dictionary<Guid, MunDistrict>();

		private void LoadMunDistricts()
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var munDistricts = session.QueryOver<MunDistrict>().List();
				foreach(var munDistrict in munDistricts)
				{
					_munDistricts.Add(munDistrict.FiasGuid, munDistrict);
				}
			}
		}

		private void ProcessHierarchyObjects(IEnumerable<IFiasHierarchy> hierarchyObjects)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var hierarchyNodes = FillHierarchyObjects(hierarchyObjects);

				var currentObjectGuids = hierarchyNodes.Select(x => x.CurrentObjectGuid);
				var parentObjectGuids = hierarchyNodes.Select(x => x.ParentObjectGuid);
				var streetGuids = GetStreetGuids(currentObjectGuids.ToArray());
				var mudDistrictHierarchyCache = ContainsStreetMunDistrictHierarchy(currentObjectGuids.ToArray());

				foreach(var hierarchyNode in hierarchyNodes)
				{
					var streetGuid = hierarchyNode.CurrentObjectGuid;
					var parentGuid = hierarchyNode.ParentObjectGuid;

					if(mudDistrictHierarchyCache.Contains(streetGuid))
					{
						continue;
					}


					if(!streetGuids.Contains(streetGuid))
					{
						continue;
					}

					if(!_munDistricts.ContainsKey(parentGuid))
					{
						if(hierarchyNode.ParentLevel > 3)
						{
							var otherHierarchy = new StreetOtherHierarchy();
							otherHierarchy.FiasStreetGuid = streetGuid;
							otherHierarchy.FiasParentGuid = parentGuid;
							session.SaveOrUpdate(otherHierarchy);
						}
						continue;
					}

					var hierarchy = new StreetMunDistrictHierarchy();
					hierarchy.FiasStreetGuid = streetGuid;
					hierarchy.FiasMunDistrictGuid = parentGuid;
					session.SaveOrUpdate(hierarchy);
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private void ProcessHierarchyIntermediateObjects(IEnumerable<IFiasHierarchy> hierarchyObjects, bool secondIter)
		{
			using(var session = _sessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var hierarchyNodes = FillHierarchyObjects(hierarchyObjects);

				var currentObjectGuids = hierarchyNodes.Select(x => x.CurrentObjectGuid);
				var parentObjectGuids = hierarchyNodes.Select(x => x.ParentObjectGuid);
				var streetOtherParentGuids = GetStreetOtherGuids(currentObjectGuids.ToArray());
				var cities = GetCities(parentObjectGuids.ToArray());

				foreach(var hierarchyNode in hierarchyNodes)
				{
					var objectGuid = hierarchyNode.CurrentObjectGuid;
					var parentGuid = hierarchyNode.ParentObjectGuid;

					if(!streetOtherParentGuids.Contains(objectGuid))
					{
						continue;
					}

					if(!_munDistricts.ContainsKey(parentGuid))
					{
						if(secondIter)
						{
							continue;
						}

						if(cities.Contains(hierarchyNode.ParentObjectGuid))
						{
							continue;
						}

						if(hierarchyNode.ParentLevel > 3)
						{
							var matchedStreets1 = GetStreetOtherByParentGuid(objectGuid);

							foreach(var streetOther in matchedStreets1)
							{
								var otherHierarchy = new StreetOtherHierarchy();
								otherHierarchy.FiasStreetGuid = streetOther.FiasStreetGuid;
								otherHierarchy.FiasParentGuid = parentGuid;
								session.SaveOrUpdate(otherHierarchy);
							}
						}
						continue;
					}

					var matchedStreets = GetStreetOtherByParentGuid(objectGuid);

					foreach(var streetOther in matchedStreets)
					{
						var hierarchy = new StreetMunDistrictHierarchy();
						hierarchy.FiasStreetGuid = streetOther.FiasStreetGuid;
						hierarchy.FiasMunDistrictGuid = parentGuid;
						session.SaveOrUpdate(hierarchy);
					}
				}

				session.Flush();
				transaction.Commit();
			}
		}

		private HashSet<Guid> GetStreetOtherGuids(Guid[] guids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<StreetOtherHierarchy>()
					.WhereRestrictionOn(x => x.FiasParentGuid).IsIn(guids)
					.Select(Projections.Distinct(Projections.Property<StreetOtherHierarchy>(x => x.FiasParentGuid)))
					.List<Guid>()
					.ToHashSet();
				return result;
			}
		}

		private HashSet<Guid> GetCities(Guid[] guids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<City>()
					.WhereRestrictionOn(x => x.FiasCityGuid).IsIn(guids)
					.Select(Projections.Distinct(Projections.Property<City>(x => x.FiasCityGuid)))
					.List<Guid>()
					.ToHashSet();
				return result;
			}
		}

		private IList<StreetOtherHierarchy> GetStreetOtherByParentGuid(Guid guid)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<StreetOtherHierarchy>()
					.Where(x => x.FiasParentGuid == guid)
					.List<StreetOtherHierarchy>();
				return result;
			}
		}

		private bool ContainsStreetMunDistrictHierarchy(Guid streetGuid)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<StreetMunDistrictHierarchy>()
					.Where(x => x.FiasStreetGuid == streetGuid)
					.List();
				return result.Any();
			}
		}

		private HashSet<Guid> ContainsStreetMunDistrictHierarchy(Guid[] streetGuids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<StreetMunDistrictHierarchy>()
					.WhereRestrictionOn(x => x.FiasStreetGuid).IsIn(streetGuids)
					.Select(Projections.Distinct(Projections.Property<StreetMunDistrictHierarchy>(x => x.FiasStreetGuid)))
					.List<Guid>()
					.ToHashSet();
				return result;
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
				var node = new HierarchyNode(current.FiasObjectGuid, parent.FiasObjectGuid, parent.Level.Level);
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

		private class HierarchyNode
		{
			public Guid CurrentObjectGuid { get; private set; }
			public Guid ParentObjectGuid { get; private set; }
			public int ParentLevel { get; private set; }

			public HierarchyNode(Guid currentObjectGuid, Guid parentObjectGuid, int parentLevel)
			{
				CurrentObjectGuid = currentObjectGuid;
				ParentObjectGuid = parentObjectGuid;
				ParentLevel = parentLevel;
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

		private IEnumerable<Street> GetStreets(long[] ids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var streets = session.QueryOver<Street>()
					.WhereRestrictionOn(x => x.FiasStreetId).IsIn(ids)
					.List<Street>();
				return streets;
			}
		}
	}
}
