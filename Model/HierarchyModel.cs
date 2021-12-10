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
	public class HierarchyModel
	{
		private readonly FiasReaderFactory _fiasReaderFactory;
		private readonly ISessionFactory _sessionFactory;
		private readonly int _batchSize = 1000;


		public HierarchyModel(FiasReaderFactory fiasReaderFactory, ISessionFactory sessionFactory)
		{
			_fiasReaderFactory = fiasReaderFactory ?? throw new ArgumentNullException(nameof(fiasReaderFactory));
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadAndUpdateHierarchy(int regionCode)
		{
			LoadAdmHierarchy(regionCode);
			LoadMunHierarchy(regionCode);
			ShrinkOtherStreetParents();
		}

		private void LoadAdmHierarchy(int regionCode)
		{
			using(var fiasReader = _fiasReaderFactory.GetReader<FiasAdmHierarchy>(regionCode))
			{
				int processedCounter = 0;
				List<FiasAdmHierarchy> batch = new List<FiasAdmHierarchy>();
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
						Console.Write($"\rЗагрузка Адм-территориальной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
						batch = new List<FiasAdmHierarchy>();
						loadCount = 0;
					}
				}
				ProcessHierarchyObjects(batch);
				processedCounter += batch.Count;
				Console.WriteLine($"\rЗагрузка Адм-территориальной иерархии. Регион {regionCode}. Загружено {processedCounter} объектов.");
			}
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
				ProcessCityLayer(session, hierarchy);
				ProcessStreetLayer(session, hierarchy);
				ProcessSteadLayer(session, hierarchy);
				ProcessHouseLayer(session, hierarchy);
				ProcessApartmentLayer(session, hierarchy);
				ProcessOtherHierarchy(session, hierarchy);
				session.Flush();
				transaction.Commit();
			}
		}

		private void ShrinkOtherStreetParents()
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var allHierarchy = GetStreetOtherHierarchy();
				int counter = 0;
				while(allHierarchy.Any())
				{
					var firstItem = allHierarchy.First();
					var part = allHierarchy.Where(x => x.FiasParentGuid == firstItem.FiasParentGuid).ToList();

					foreach(var item in part)
					{
						allHierarchy.Remove(item);
					}

					ShrinkNextPart(part);

					counter++;
					Console.Write($"\rПерепривязка не связанных напрямую улиц к городам. Осталось {allHierarchy.Count} объектов.");
				}
			}
			Console.WriteLine();
		}

		private void ShrinkNextPart(IEnumerable<StreetOtherHierarchy> part)
		{
			if(!part.Any())
			{
				return;
			}
			var firstItem = part.FirstOrDefault();
			if(!part.All(x => x.FiasParentGuid == firstItem.FiasParentGuid))
			{
				throw new InvalidOperationException("Все элементы иерархии в обрабатываемой партии должны иметь одного родителя");
			}
			

			var streetCityParentHierarchy = GetStreetCityHierarchy(firstItem.FiasParentGuid);
			if(streetCityParentHierarchy != null)
			{
				using(var session = _sessionFactory.OpenSession())
				using(var transaction = session.BeginTransaction())
				{
					foreach(var item in part)
					{
						var streetCityHierarchy = new StreetCityHierarchy();
						streetCityHierarchy.FiasStreetGuid = item.FiasStreetGuid;
						streetCityHierarchy.FiasCityGuid = streetCityParentHierarchy.FiasCityGuid;
						session.Delete(item);
						session.SaveOrUpdate(streetCityHierarchy);
					}
					session.Delete(streetCityParentHierarchy);
					session.Flush();
					transaction.Commit();
				}
			}
			else
			{
				//Если это не город то эта иерархия не нужна, так как там район, который ничем не поможет.
				//а настроящая связь к населенному пункту находится в другой иерархии
				using(var session = _sessionFactory.OpenSession())
				using(var transaction = session.BeginTransaction())
				{
					foreach(var item in part)
					{
						session.Delete(item);
					}
					session.Flush();
					transaction.Commit();
				}
			}
		}

		private bool IsCity(Guid guid)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<City>()
				.Where(x => x.FiasCityGuid == guid)
				.List();
				return result.Any();
			}
		}

		private StreetOtherHierarchy GetStreetOtherHierarchy(Guid guid)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<StreetOtherHierarchy>()
				.Where(x => x.FiasStreetGuid == guid)
				.SingleOrDefault();
				return result;
			}
		}

		private StreetCityHierarchy GetStreetCityHierarchy(Guid guid)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<StreetCityHierarchy>()
				.Where(x => x.FiasStreetGuid == guid)
				.List()
				.FirstOrDefault();
				return result;
			}
		}

		private OtherHierarchy GetOtherHierarchy(Guid guid)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<OtherHierarchy>()
					.Where(x => x.FiasGuid == guid)
					.SingleOrDefault();
				return result;
			}
		}

		private IList<StreetOtherHierarchy> GetStreetOtherHierarchy()
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var result = session.QueryOver<StreetOtherHierarchy>().List();
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

		#region City

		private void ProcessCityLayer(ISession session, HashSet<HierarchyNode> hierarchy)
		{
			if(!hierarchy.Any())
			{
				return;
			}

			var currentObjectGuids = hierarchy.Select(x => x.CurrentObjectGuid);

			var cityGuids = GetCityGuids(currentObjectGuids.ToArray());

			foreach(var hierarchyNode in hierarchy.ToList())
			{
				var cityGuid = hierarchyNode.CurrentObjectGuid;
				var parentGuid = hierarchyNode.ParentObjectGuid;

				if(!cityGuids.Contains(cityGuid))
				{
					continue;
				}
				hierarchy.Remove(hierarchyNode);

				var cityHierarchy = new CityHierarchy();
				cityHierarchy.FiasCityGuid = cityGuid;
				cityHierarchy.FiasParentObjectGuid = parentGuid;

				session.SaveOrUpdate(cityHierarchy);
			}
		}

		private HashSet<Guid> GetCityGuids(Guid[] guids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var cityGuids = session.QueryOver<City>()
					.WhereRestrictionOn(x => x.FiasCityGuid).IsIn(guids)
					.Select(Projections.Distinct(Projections.Property<City>(x => x.FiasCityGuid)))
					.List<Guid>()
					.ToHashSet();
				return cityGuids;
			}
		}

		#endregion

		#region Street

		private void ProcessStreetLayer(ISession session, HashSet<HierarchyNode> hierarchy)
		{
			if(!hierarchy.Any())
			{
				return;
			}

			var currentObjectGuids = hierarchy.Select(x => x.CurrentObjectGuid);
			var parentObjectGuids = hierarchy.Select(x => x.ParentObjectGuid);
			var streetGuids = GetStreetGuids(currentObjectGuids.ToArray());
			var cityGuids = GetCityGuids(parentObjectGuids.ToArray());

			foreach(var hierarchyNode in hierarchy.ToList())
			{
				var streetGuid = hierarchyNode.CurrentObjectGuid;
				var parentGuid = hierarchyNode.ParentObjectGuid;

				if(!streetGuids.Contains(streetGuid))
				{
					continue;
				}
				hierarchy.Remove(hierarchyNode);

				if(cityGuids.Contains(parentGuid))
				{

					var streetCityHierarchy = new StreetCityHierarchy();
					streetCityHierarchy.FiasStreetGuid = streetGuid;
					streetCityHierarchy.FiasCityGuid = parentGuid;
					session.SaveOrUpdate(streetCityHierarchy);
				}
				else
				{
					var streetOtherHierarchy = new StreetOtherHierarchy();
					streetOtherHierarchy.FiasStreetGuid = streetGuid;
					streetOtherHierarchy.FiasParentGuid = parentGuid;
					session.SaveOrUpdate(streetOtherHierarchy);
				}
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

		#region Stead

		private void ProcessSteadLayer(ISession session, HashSet<HierarchyNode> hierarchy)
		{
			if(!hierarchy.Any())
			{
				return;
			}

			var currentObjectGuids = hierarchy.Select(x => x.CurrentObjectGuid);
			var parentObjectGuids = hierarchy.Select(x => x.ParentObjectGuid);
			var steadGuids = GetSteadGuids(currentObjectGuids.ToArray());
			var streetGuids = GetStreetGuids(parentObjectGuids.ToArray());
			var cityGuids = GetCityGuids(parentObjectGuids.ToArray());

			foreach(var hierarchyNode in hierarchy.ToList())
			{
				var steadGuid = hierarchyNode.CurrentObjectGuid;
				var parentGuid = hierarchyNode.ParentObjectGuid;

				if(!steadGuids.Contains(steadGuid))
				{
					continue;
				}
				hierarchy.Remove(hierarchyNode);

				if(streetGuids.Contains(parentGuid))
				{

					var steadStreetHierarchy = new SteadStreetHierarchy();
					steadStreetHierarchy.FiasSteadGuid = steadGuid;
					steadStreetHierarchy.FiasStreetGuid = parentGuid;
					session.SaveOrUpdate(steadStreetHierarchy);
				}
				else if(cityGuids.Contains(parentGuid))
				{
					var steadCityHierarchy = new SteadCityHierarchy();
					steadCityHierarchy.FiasSteadGuid = steadGuid;
					steadCityHierarchy.FiasCityGuid = parentGuid;
					session.SaveOrUpdate(steadCityHierarchy);
				}
			}
		}

		private HashSet<Guid> GetSteadGuids(Guid[] guids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var cityGuids = session.QueryOver<Stead>()
					.WhereRestrictionOn(x => x.FiasSteadGuid).IsIn(guids)
					.Select(Projections.Distinct(Projections.Property<Stead>(x => x.FiasSteadGuid)))
					.List<Guid>()
					.ToHashSet();
				return cityGuids;
			}
		}

		#endregion

		#region House

		private void ProcessHouseLayer(ISession session, HashSet<HierarchyNode> hierarchy)
		{
			if(!hierarchy.Any())
			{
				return;
			}

			var currentObjectGuids = hierarchy.Select(x => x.CurrentObjectGuid);
			var parentObjectGuids = hierarchy.Select(x => x.ParentObjectGuid);
			var houseGuids = GetHouseGuids(currentObjectGuids.ToArray());
			var streetGuids = GetStreetGuids(parentObjectGuids.ToArray());
			var cityGuids = GetCityGuids(parentObjectGuids.ToArray());

			foreach(var hierarchyNode in hierarchy.ToList())
			{
				var houseGuid = hierarchyNode.CurrentObjectGuid;
				var parentGuid = hierarchyNode.ParentObjectGuid;

				if(!houseGuids.Contains(houseGuid))
				{
					continue;
				}

				hierarchy.Remove(hierarchyNode);

				if(streetGuids.Contains(parentGuid))
				{
					var houseStreetHierarchy = new HouseStreetHierarchy();
					houseStreetHierarchy.FiasHouseGuid = houseGuid;
					houseStreetHierarchy.FiasStreetGuid = parentGuid;
					session.SaveOrUpdate(houseStreetHierarchy);
				}
				else if(cityGuids.Contains(parentGuid))
				{
					var houseCityHierarchy = new HouseCityHierarchy();
					houseCityHierarchy.FiasHouseGuid = houseGuid;
					houseCityHierarchy.FiasCityGuid = parentGuid;
					session.SaveOrUpdate(houseCityHierarchy);
				}
			}
		}

		private HashSet<Guid> GetHouseGuids(Guid[] guids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var houseGuids = session.QueryOver<House>()
					.WhereRestrictionOn(x => x.FiasHouseGuid).IsIn(guids)
					.Select(Projections.Distinct(Projections.Property<House>(x => x.FiasHouseGuid)))
					.List<Guid>()
					.ToHashSet();
				return houseGuids;
			}
		}

		#endregion

		#region Apartment

		private void ProcessApartmentLayer(ISession session, HashSet<HierarchyNode> hierarchy)
		{
			if(!hierarchy.Any())
			{
				return;
			}

			var currentObjectGuids = hierarchy.Select(x => x.CurrentObjectGuid);
			var parentObjectGuids = hierarchy.Select(x => x.ParentObjectGuid);
			var apartmentGuids = GetApartmentGuids(currentObjectGuids.ToArray());
			var houseGuids = GetHouseGuids(parentObjectGuids.ToArray());

			foreach(var hierarchyNode in hierarchy.ToList())
			{
				var apartmentGuid = hierarchyNode.CurrentObjectGuid;
				var parentGuid = hierarchyNode.ParentObjectGuid;

				if(!apartmentGuids.Contains(apartmentGuid))
				{
					continue;
				}

				hierarchy.Remove(hierarchyNode);

				if(houseGuids.Contains(parentGuid))
				{
					var apartmentHouseHierarchy = new ApartmentHouseHierarchy();
					apartmentHouseHierarchy.FiasApartmentGuid = apartmentGuid;
					apartmentHouseHierarchy.FiasHouseGuid = parentGuid;
					session.SaveOrUpdate(apartmentHouseHierarchy);
				}
				else
				{
					throw new InvalidOperationException("Помещение должно быть подчинено дому");

				}
			}
		}

		private HashSet<Guid> GetApartmentGuids(Guid[] guids)
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var apartmentGuids = session.QueryOver<Apartment>()
					.WhereRestrictionOn(x => x.FiasApartmentGuid).IsIn(guids)
					.Select(Projections.Distinct(Projections.Property<Apartment>(x => x.FiasApartmentGuid)))
					.List<Guid>()
					.ToHashSet();
				return apartmentGuids;
			}
		}

		#endregion

		#region Other

		private void ProcessOtherHierarchy(ISession session, HashSet<HierarchyNode> hierarchy)
		{
			if(!hierarchy.Any())
			{
				return;
			}

			foreach(var hierarchyNode in hierarchy)
			{
				var otherHierarchy = new OtherHierarchy();
				otherHierarchy.FiasGuid = hierarchyNode.CurrentObjectGuid;
				otherHierarchy.FiasParentGuid = hierarchyNode.ParentObjectGuid;
				session.SaveOrUpdate(otherHierarchy);
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
