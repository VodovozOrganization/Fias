using Fias.Domain.Entities;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Transform;
using Npgsql;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using NetTopologySuite.Geometries;
using FluentNHibernate.Mapping;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries.Utilities;
using System.Linq;
using Fias.Search;
using Fias.Search.DTO;
using System.Data;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Fias.Source;
using Fias.LoadModel;

namespace ConsoleTest
{
	public class OsmScript : IDisposable
	{
		SshClient _sshClient;
		private ISessionFactory _fiasSessionFactory;
		private ISessionFactory _osmSessionFactory;
		private IEnumerable<StreetTypeNode> streetTypes = null;
		Dictionary<string, StreetNode> cleanStreets = new Dictionary<string, StreetNode>();

		public OsmScript()
		{
			PrivateKeyFile privateKeyFile = new PrivateKeyFile(@"C:\Users\Enzo\Desktop\keys\osm_openssh_private");
			ConnectionInfo connectionInfo = new PrivateKeyConnectionInfo("srv2.vod.qsolution.ru", 2208, "root", privateKeyFile);
			connectionInfo.Timeout = TimeSpan.FromSeconds(30);

			_sshClient = new SshClient(connectionInfo);

			try
			{
				Console.WriteLine("Trying SSH connection...");
				_sshClient.Connect();
				if(_sshClient.IsConnected)
				{
					Console.WriteLine("SSH connection is active: {0}", _sshClient.ConnectionInfo.ToString());
				}
				else
				{
					Console.WriteLine("SSH connection has failed: {0}", _sshClient.ConnectionInfo.ToString());
				}

				Console.WriteLine("\r\nTrying port forwarding...");
				var portFwld = new ForwardedPortLocal(IPAddress.Loopback.ToString(), 5433, IPAddress.Loopback.ToString(), 5432);
				_sshClient.AddForwardedPort(portFwld);
				portFwld.Start();
				if(portFwld.IsStarted)
				{
					Console.WriteLine("Port forwarded: {0}", portFwld.ToString());
				}
				else
				{
					Console.WriteLine("Port forwarding has failed.");
				}
			}
			catch(SshException e)
			{
				Console.WriteLine("SSH client connection error: {0}", e.Message);
			}
			catch(System.Net.Sockets.SocketException e)
			{
				Console.WriteLine("Socket connection error: {0}", e.Message);
			}

			ConfigureFiasDatabaseConnection();
			ConfigureOsmDatabaseConnection();
		}

		public void Dispose()
		{
			_sshClient?.Dispose();
			_fiasSessionFactory?.Dispose();
			_osmSessionFactory?.Dispose();
		}
		
		private void ConfigureFiasDatabaseConnection()
		{
			var connectionBuilder = new NpgsqlConnectionStringBuilder();
			connectionBuilder.Host = "localhost";
			connectionBuilder.Port = 5433;
			connectionBuilder.Database = "fias";
			connectionBuilder.Username = "postgres";
			connectionBuilder.Password = "";
			connectionBuilder.SslMode = SslMode.Disable;

			var databaseConfig = PostgreSQLConfiguration.Standard
				.Dialect<PostgreSQL83Dialect>()
				.AdoNetBatchSize(100)
				.ConnectionString(connectionBuilder.ConnectionString)
				//.Driver<LoggedNpgsqlDriver>()
				;

			var fluenConfig = Fluently.Configure().Database(databaseConfig);

			var mapAssembly = Assembly.GetAssembly(typeof(Fias.Domain.AssemblyFinder));

			fluenConfig.Mappings(m =>
			{
				m.FluentMappings.AddFromAssembly(mapAssembly);
			});
			_fiasSessionFactory = fluenConfig.BuildSessionFactory();
		}
		
		private void ConfigureOsmDatabaseConnection()
		{
			var connectionBuilder = new NpgsqlConnectionStringBuilder();
			connectionBuilder.Host = "localhost";
			connectionBuilder.Port = 5433;
			connectionBuilder.Database = "osmgis";
			connectionBuilder.Username = "postgres";
			connectionBuilder.Password = "";
			connectionBuilder.SslMode = SslMode.Disable;

			var databaseConfig = PostgreSQLConfiguration.Standard
				//.Dialect<NHibernate.Spatial.Dialect.PostGisDialect>()
				.Dialect<PostgreSQL83Dialect>()
				.AdoNetBatchSize(100)
				.ConnectionString(connectionBuilder.ConnectionString)
				//.Driver<LoggedNpgsqlDriver>()
				;

			var fluenConfig = Fluently.Configure().Database(databaseConfig);
			_osmSessionFactory = fluenConfig.BuildSessionFactory();
		}

		public void Start()
		{
			Console.WriteLine("Обработка координат из OSM");
			MovingAverage averageTime = new MovingAverage();
			Stopwatch sw = new Stopwatch();
			OsmAddressProvider osmAddressProvider = new OsmAddressProvider(_osmSessionFactory);
			FiasAddressProvider fiasAddressProvider = new FiasAddressProvider(_fiasSessionFactory);
			int totalLoaded = 0;
			int totalLenLoaded = 0;
			int totalFailed = 0;
			int totalSuccessed = 0;
			bool hasAddresses = false;

			sw.Start();
			do
			{
				if(totalLoaded > 0)
				{
					sw.Stop();
					Console.Write($"\rЗагружено {totalLoaded} из ~603000. Подходящих: {totalLenLoaded}. Успешно {totalSuccessed}. Не найдено {totalFailed}. Ср. время: {averageTime.ComputeAverage((decimal)Math.Round(sw.Elapsed.TotalSeconds, 2))} сек.");
					sw.Restart();
				}

				var addresses = osmAddressProvider.GetNext();
				hasAddresses = addresses.Any();
				totalLoaded += addresses.Count;

				var lenAddresses = addresses.Where(x => x.IsLen);
				if(!lenAddresses.Any())
				{
					continue;
				}
				totalLenLoaded += lenAddresses.Count();

				foreach(var lenAddress in lenAddresses)
				{
					var city = lenAddress.City;
					var street = GetCleanStreet(lenAddress.Street);
					if(!street.HasType)
					{
						FailAddress(lenAddress, "Не найден адрес");
						continue;
					}

					var house = ParseHouse(lenAddress.House);
					var houseGuid = fiasAddressProvider.GetHouseGuid(city, street.StreetTypes, street.Street, house.House, house.Corpus, house.Building);
					if(houseGuid.HasValue)
					{
						SaveCoordinates(houseGuid.Value, lenAddress);
						totalSuccessed++;
					}
					else
					{
						FailAddress(lenAddress, "Не найден дом");
						totalFailed++;
					}
				}

			} while(hasAddresses);
			Console.WriteLine($"\nЗагружено {totalLoaded} из ~603000. Подходящих: {totalLenLoaded}. Успешно {totalSuccessed}. Не найдено {totalFailed}");
		}

		BlockingCollection<IEnumerable<OsmAddress>> queue = new BlockingCollection<IEnumerable<OsmAddress>>(4);
		bool inProgress = true;

		int totalLoaded = 0;

		int totalFailed1 = 0;
		int totalSuccessed1 = 0;

		int totalFailed2 = 0;
		int totalSuccessed2 = 0;

		private void Log()
		{
			var failed = totalFailed1 + totalFailed2;
			var successed = totalSuccessed1 + totalSuccessed2;
			Console.Write($"\rЗагружено {totalLoaded} из ~603000. Успешно {successed}. Не найдено {failed}.");
		}

		public void StartAsync()
		{
			var garPath = @"C:\Users\Enzo\Downloads\gar_xml.zip";

			var fileStream = new FileStream(garPath, FileMode.Open, FileAccess.Read);
			var garFile = new ZipFile(fileStream);
			FiasReaderFactory readerFactory = new FiasReaderFactory(garFile);
			/*HierarchyDistrictModel hierarchyDistrictModel = new HierarchyDistrictModel(readerFactory, _fiasSessionFactory);
			hierarchyDistrictModel.LoadAndUpdateHierarchy(78);*/

			HierarchyAdditionalStreetModel hierarchyAdditionalStreetModel = new HierarchyAdditionalStreetModel(readerFactory, _fiasSessionFactory);
			hierarchyAdditionalStreetModel.LoadAndUpdateHierarchy(78);
		}

		public void StartAsyncs()
		{
			Console.WriteLine("Обработка координат из OSM");
			//MovingAverage averageTime = new MovingAverage();
			//Stopwatch sw = new Stopwatch();
			OsmAddressProvider osmAddressProvider = new OsmAddressProvider(_osmSessionFactory);
			//FiasAddressProvider fiasAddressProvider = new FiasAddressProvider(_fiasSessionFactory);
			//int totalLenLoaded = 0;
			//int totalFailed = 0;
			//int totalSuccessed = 0;
			bool hasAddresses = false;
			System.Timers.Timer timer = new System.Timers.Timer(2000);
			timer.Elapsed += (s, e) => Log();
			timer.Start();
			CancellationTokenSource cts = new CancellationTokenSource();
			Task task1 = Task.Run(() => ProcessAddresses(ref totalFailed1, ref totalSuccessed1, cts.Token));
			Task task2 = Task.Run(() => ProcessAddresses(ref totalFailed2, ref totalSuccessed2, cts.Token));

			//sw.Start();
			do
			{
				//if(totalLoaded > 0)
				//{
				//	//sw.Stop();
				//	//Console.Write($"\rЗагружено {totalLoaded} из ~603000. Подходящих: {totalLenLoaded}. Успешно {totalSuccessed}. Не найдено {totalFailed}. Ср. время: {averageTime.ComputeAverage((decimal)Math.Round(sw.Elapsed.TotalSeconds, 2))} сек.");
				//	//Console.Write($"\rЗагружено {totalLoaded} из ~603000.");
				//	//sw.Restart();
				//}

				var addresses = osmAddressProvider.GetNext();
				hasAddresses = addresses.Any();
				inProgress = hasAddresses;
				totalLoaded += addresses.Count;

				var lenAddresses = addresses.Where(x => x.IsLen);
				if(!lenAddresses.Any())
				{
					continue;
				}
				queue.Add(lenAddresses);
			} while(hasAddresses);

			Task.WhenAll(task1, task2);
			timer.Stop();
			cts.Dispose();
			//Console.WriteLine($"\nЗагружено {totalLoaded} из ~603000. Подходящих: {totalLenLoaded}. Успешно {totalSuccessed}. Не найдено {totalFailed}");
		}

		private void ProcessAddresses(ref int failed, ref int successed, CancellationToken token)
		{
			FiasAddressProvider fiasAddressProvider = new FiasAddressProvider(_fiasSessionFactory);
			do
			{
				var addresses = queue.Take();

				if(!addresses.Any())
				{
					continue;
				}

				foreach(var lenAddress in addresses)
				{
					var city = lenAddress.City;
					var street = GetCleanStreet(lenAddress.Street);
					if(!street.HasType)
					{
						FailAddress(lenAddress, "Не найден адрес");
						continue;
					}

					var house = ParseHouse(lenAddress.House);
					var houseGuid = fiasAddressProvider.GetHouseGuid(city, street.StreetTypes, street.Street, house.House, house.Corpus, house.Building);
					if(houseGuid.HasValue)
					{
						SaveCoordinates(houseGuid.Value, lenAddress);
						successed++;
					}
					else
					{
						FailAddress(lenAddress, "Не найден дом");
						failed++;
					}
				}
			} while(!token.IsCancellationRequested);
		}



		private void FailAddress(OsmAddress osmAddress, string reason)
		{
			using(var session = _fiasSessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var failedAddress = new WrongOsmAddress();
				failedAddress.CityName = osmAddress.City;
				failedAddress.StreetName = osmAddress.Street;
				failedAddress.HouseNumber = osmAddress.House;
				failedAddress.Latitude = (decimal)osmAddress.Latitude;
				failedAddress.Longitude = (decimal)osmAddress.Longitude;
				failedAddress.Reason = reason;
				session.SaveOrUpdate(failedAddress);
				session.Flush();
				transaction.Commit();
			}
		}

		private void SaveCoordinates(Guid houseGuid, OsmAddress osmAddress)
		{
			using(var session = _fiasSessionFactory.OpenSession())
			using(var transaction = session.BeginTransaction(IsolationLevel.RepeatableRead))
			{
				var houseCoordinate = new HouseCoordinate();
				houseCoordinate.HouseFiasGuid = houseGuid;
				houseCoordinate.Latitude = (decimal)osmAddress.Latitude;
				houseCoordinate.Longitude = (decimal)osmAddress.Longitude;
				session.SaveOrUpdate(houseCoordinate);
				session.Flush();
				transaction.Commit();
			}
		}

		public StreetNode GetCleanStreet(string street)
		{
			if(cleanStreets.TryGetValue(street, out StreetNode streetNode))
			{
				return streetNode;
			}

			var streetTypes = GetStreetTypes().OrderByDescending(x => x.TypeName.Length);

			var startWith = streetTypes.Where(x => street.ToLower().StartsWith(x.TypeName.ToLower()));
			if(startWith.Any())
			{
				var streetType = startWith.First().TypeName;
				var cleanStreet = street.Replace(streetType, "", StringComparison.OrdinalIgnoreCase).Trim();
				streetNode = new StreetNode(startWith.Select(x => x.Id).ToArray(), cleanStreet);
				cleanStreets.Add(street, streetNode);
				return streetNode;
			}

			var endWith = streetTypes.Where(x => street.ToLower().EndsWith(x.TypeName.ToLower()));
			if(endWith.Any())
			{
				var streetType = endWith.First().TypeName;
				var cleanStreet = street.Replace(streetType, "", StringComparison.OrdinalIgnoreCase).Trim();
				streetNode = new StreetNode(endWith.Select(x => x.Id).ToArray(), cleanStreet);
				cleanStreets.Add(street, streetNode);
				return streetNode;
			}

			streetNode = new StreetNode(new int[0], street);
			cleanStreets.Add(street, streetNode);
			return streetNode;
		}

		public class StreetNode
		{
			public int[] StreetTypes { get; set; }
			public string Street { get; set; }

			public bool HasType => StreetTypes.Any();

			public StreetNode(int[] streetTypes, string street)
			{
				StreetTypes = streetTypes;
				Street = street;
			}
		}

		private IEnumerable<StreetTypeNode> GetStreetTypes()
		{
			if(streetTypes != null)
			{
				return streetTypes;
			}

			using(var session = _fiasSessionFactory.OpenSession())
			{
				var sql = $@"SELECT st.id as {nameof(StreetTypeNode.Id)}, st.""name"" as {nameof(StreetTypeNode.TypeName)} FROM public.street_types st";
				var query = session.CreateSQLQuery(sql);
				query.SetResultTransformer(Transformers.AliasToBean<StreetTypeNode>());
				streetTypes = query.List<StreetTypeNode>();
			}

			return streetTypes;
		}

		public class StreetTypeNode
		{
			public int Id { get; set; }
			public string TypeName { get; set; }

			public StreetTypeNode()
			{

			}

			public StreetTypeNode(int id, string typeName)
			{
				Id = id;
				TypeName = typeName;
			}
		}

		public HouseNode ParseHouse(string houseString)
		{
			string[] corpusStrings = new[] { "корпус", "кор", "к" };
			string[] buildingStrings = new[] { "строение", "стр", "с" };
			char[] specialChars = " .,/?!\\|\"'`~<>()&^$;:*%#@[]{}-_+=".ToArray();

			int corpusIndex = -1;
			foreach(var corpusString in corpusStrings)
			{
				corpusIndex = houseString.IndexOf(corpusString);
				if(corpusIndex > -1)
				{
					break;
				}
			}

			int buildingIndex = -1;
			foreach(var buildingString in buildingStrings)
			{
				buildingIndex = houseString.IndexOf(buildingString);
				if(buildingIndex > -1)
				{
					break;
				}
			}

			bool hasCorpus = corpusIndex > -1;
			bool hasBuilding = buildingIndex > -1;

			string houseSubString = houseString;
			string corpusSubString = "";
			string buildingSubString = "";

			if(hasCorpus && hasBuilding)
			{
				if(corpusIndex < buildingIndex)
				{
					var length = buildingIndex - corpusIndex;
					corpusSubString = houseString.Substring(corpusIndex, length);
					buildingSubString = houseString.Substring(buildingIndex);
					houseSubString = houseString.Substring(0, corpusIndex);
				}
				else
				{
					var length = corpusIndex - buildingIndex;
					buildingSubString = houseString.Substring(buildingIndex, length);
					corpusSubString = houseString.Substring(corpusIndex);
					houseSubString = houseString.Substring(0, buildingIndex);
				}
			}
			else if(hasCorpus)
			{
				corpusSubString = houseString.Substring(corpusIndex);
				houseSubString = houseString.Substring(0, corpusIndex);
			}
			else if(hasBuilding)
			{
				buildingSubString = houseString.Substring(buildingIndex);
				houseSubString = houseString.Substring(0, buildingIndex);
			}

			houseSubString = houseSubString.Trim(specialChars);
			corpusSubString = corpusSubString.Trim(specialChars);
			buildingSubString = buildingSubString.Trim(specialChars);

			var result = new HouseNode(houseSubString, corpusSubString, buildingSubString);
			return result;
		}

		public class HouseNode
		{
			public string House { get; set; }
			public string Corpus { get; set; }
			//public bool HasCorpus => !string.IsNullOrWhiteSpace(Corpus);
			public string Building { get; set; }
			//public bool HasBuilding => !string.IsNullOrWhiteSpace(Building);

			public HouseNode()
			{

			}

			public HouseNode(string house, string corpus, string building)
			{
				House = house;
				Corpus = corpus;
				Building = building;
			}
		}
	}

	public class MovingAverage
	{
		private Queue<decimal> samples = new Queue<decimal>();
		private int windowSize = 20;
		private decimal sampleAccumulator;
		public decimal Average { get; private set; }

		/// <summary>
		/// Computes a new windowed average each time a new sample arrives
		/// </summary>
		/// <param name="newSample"></param>
		public decimal ComputeAverage(decimal newSample)
		{
			sampleAccumulator += newSample;
			samples.Enqueue(newSample);

			if(samples.Count > windowSize)
			{
				sampleAccumulator -= samples.Dequeue();
			}

			Average = sampleAccumulator / samples.Count;
			return Average;
		}
	}

	public class FiasAddressProvider
	{
		private readonly ISessionFactory _sessionFactory;
		private readonly CityRepository _cityRepository;
		private readonly StreetRepository _streetRepository;
		private readonly HouseRepository _houseRepository;

		public FiasAddressProvider(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
			_cityRepository = new CityRepository(_sessionFactory);
			_streetRepository = new StreetRepository(_sessionFactory);
			_houseRepository = new HouseRepository(_sessionFactory);
		}

		public Guid? GetHouseGuid(string city, int[] streetTypes, string street, string house, string corpus = "", string building = "")
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var sql = $@"SELECT
	--house.city_name,
	--house.street_name,
	--house.street_type,
	--house.id,
	--house.object_type,
	house.fias_guid
	--,ht1.short_name,
	--house.number1,
	--ht2.short_name,
	--house.number2,
	--ht3.short_name,
	--house.number3	
FROM
(
	SELECT
		house_inn.city_name,
		house_inn.street_name,
		house_inn.street_type,
		house_inn.id,
		house_inn.object_type,
		house_inn.fias_guid,
		house_inn.number1,
		--Меняем владение на корпус, домовладение на строение
		CASE
			WHEN (house_inn.type1 = 1) THEN 10
			WHEN (house_inn.type1 = 3) THEN 7
			ELSE house_inn.type1
		END AS type1,
		house_inn.number2,
		--Меняем владение на корпус, домовладение на строение
		CASE
			WHEN (house_inn.type2 = 1) THEN 10
			WHEN (house_inn.type2 = 3) THEN 7
			ELSE house_inn.type2
		END AS type2,
		house_inn.number3,
		--Меняем владение на корпус, домовладение на строение
		CASE
			WHEN (house_inn.type3 = 1) THEN 10
			WHEN (house_inn.type3 = 3) THEN 7
			ELSE house_inn.type3
		END AS type3
	FROM
	(
		SELECT
			c.""name"" AS city_name,
			str.""name"" AS street_name,
			str.type_id AS street_type,
			h.id,
			'house' AS object_type,
			h.fias_house_guid AS fias_guid,
			--Номера корпусов почему-то записано в основном номере а номера домов в первом добавочном, поэтому
			--Меняем номера местами(основной со вторым добавочным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.add_number_1
			   ELSE h.""number""
			END AS number1,
			--Меняем типы местами(основной со вторым добавочным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.add_type_1
			   ELSE h.house_type
		   END AS type1,
			--Меняем номера местами(второй добавочный с основным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.""number""
				ELSE h.add_number_1
			END AS number2,
			--Меняем типы местами(второй добавочный с основным)
			CASE
				WHEN(h.house_type = 10 AND h.add_number_1 IS NOT NULL) THEN h.house_type
			   ELSE h.add_type_1
		   END AS type2,
		   h.add_number_2 AS number3,
			h.add_type_2 AS type3
		FROM
			cities c
			INNER JOIN street_city_hierarchy sch ON sch.fias_city_guid = c.fias_city_guid
			INNER JOIN streets str ON str.fias_street_guid = sch.fias_street_guid
			INNER JOIN house_street_hierarchy hsh ON hsh.fias_street_guid = str.fias_street_guid
			INNER JOIN houses h ON h.fias_house_guid = hsh.fias_house_guid
		WHERE
			h.is_active
			AND LOWER(c.""name"") = ('{city}')
			AND LOWER(str.""name"") = LOWER('{street}')
			AND str.type_id IN ({string.Join(", ", streetTypes)})
			--Исключаем не нужные типы помещений
			AND(h.house_type NOT IN(4, 6, 11, 12, 13, 14) OR h.house_type IS NULL)
			AND(h.add_type_1 NOT IN(4, 6, 11, 12, 13, 14) OR h.add_type_1 IS NULL)
			AND(h.add_type_2 NOT IN(4, 6, 11, 12, 13, 14) OR h.add_type_2 IS NULL)
			--Поиск дома
			AND
			(
				(
					--Если тип дома корпус, и надо поменять их с домом местами
					(LOWER(h.""number"") = LOWER('{corpus}') AND LOWER(h.add_number_1) = LOWER('{house}') AND '{corpus}' != '' AND h.house_type = 10)
					OR
					--Если тип дома не корпус, и корпус не заполнен
					(LOWER(h.""number"") = LOWER('{house}') AND '{corpus}' = '' AND h.add_number_1 IS NULL AND h.house_type != 10)
					OR
					--Если тип дома не корпус, и корпус заполнен
					(LOWER(h.""number"") = LOWER('{house}') AND LOWER(h.add_number_1) = LOWER('{corpus}') AND h.house_type != 10)
				)
				AND
				(LOWER(h.add_number_2) = LOWER('{building}') OR ('{building}' = '' AND h.add_number_2 IS NULL))
			)		
		UNION ALL
		SELECT
			c2.""name"" AS city_name,
			str2.""name"" AS street_name,
			str2.type_id AS street_type,
			s.id,
			'stead' AS object_type,
			s.fias_stead_guid AS fias_guid,
			s.""number"" AS number1,
			2 AS type1,
			NULL AS number2,
			NULL AS type2,
			NULL AS number3,
			NULL AS type3
		FROM
			cities c2
			INNER JOIN street_city_hierarchy sch2 ON sch2.fias_city_guid = c2.fias_city_guid
			INNER JOIN streets str2 ON str2.fias_street_guid = sch2.fias_street_guid
			INNER JOIN stead_street_hierarchy ssh ON ssh.fias_street_guid = str2.fias_street_guid
			INNER JOIN steads s ON s.fias_stead_guid = ssh.fias_stead_guid
		WHERE
			s.is_active
			AND LOWER(c2.""name"") = LOWER('{city}')
			AND LOWER(str2.""name"") = LOWER('{street}')
			AND str2.type_id IN ({string.Join(", ", streetTypes)})
			AND LOWER(s.""number"") = ('{house}') AND '{corpus}' = '' AND '{building}' = ''
	) house_inn
) house
LEFT JOIN house_types ht1 ON ht1.id = house.type1
LEFT JOIN house_types ht2 ON ht2.id = house.type2
LEFT JOIN house_types ht3 ON ht3.id = house.type3
";
				var query = session.CreateSQLQuery(sql);
				var result = query.List<Guid>();
				if(result.Any())
				{
					return result.First();
				}
				else
				{
					return null;
				}
			}
		}
	}



	public class OsmAddressProvider
	{
		private readonly ISessionFactory _sessionFactory;
		private int batch = 1000;
		private int offset = 0;

		public OsmAddressProvider(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public IList<OsmAddress> GetNext()
		{
			using(var session = _sessionFactory.OpenSession())
			{
				var sql = $@"
SELECT
	tt.city AS {nameof(OsmAddress.City)},
	tt.street AS {nameof(OsmAddress.Street)},
	tt.house AS {nameof(OsmAddress.House)},
	ST_Contains(
		ST_GeomFromText('POLYGON ((27.699 60.33983, 27.68834 60.43923, 27.73636 60.45519, 27.73911 60.47346, 27.75797 60.49089, 27.75421 60.50729, 27.76503 60.52525, 27.76245 60.5404, 27.81866 60.56062, 27.83643 60.5867, 27.89978 60.6301, 28.03695 60.69932, 28.12347 60.74296, 28.16301 60.78214, 28.2225 60.78933, 28.30356 60.84352, 28.32001 60.86029, 28.35706 60.87098, 28.46402 60.92567, 28.50747 60.94996, 28.51879 60.95544, 28.52496 60.95761, 28.53081 60.95594, 28.54488 60.95661, 28.56514 60.95494, 28.60357 60.95652, 28.63827 60.95411, 28.65784 60.95578, 28.68187 60.97827, 28.68973 60.99128, 28.69968 61.00859, 28.70539 61.02047, 28.70979 61.03196, 28.71241 61.0446, 28.78387 61.08482, 28.80132 61.09363, 28.80718 61.0973, 28.8171 61.12258, 28.87107 61.13562, 28.88352 61.14375, 28.90537 61.14605, 28.9152 61.14491, 28.94142 61.14903, 28.95305 61.1538, 28.98638 61.17618, 29.03746 61.1964, 29.14603 61.23308, 29.1711 61.24283, 29.1766 61.24448, 29.19582 61.25472, 29.2022 61.25852, 29.22669 61.26701, 29.23977 61.27073, 29.24009 61.27794, 29.26199 61.29097, 29.30689 61.33107, 29.31538 61.32497, 29.32186 61.31756, 29.3239 61.3155, 29.32748 61.31352, 29.33005 61.3106, 29.3347 61.31023, 29.33487 61.30691, 29.34019 61.30444, 29.34397 61.29968, 29.345 61.29667, 29.35195 61.29469, 29.35735 61.29136, 29.36712 61.27949, 29.37756 61.27862, 29.38506 61.27547, 29.39263 61.27246, 29.39747 61.27022, 29.4037 61.26669, 29.40508 61.26371, 29.41023 61.26268, 29.41418 61.26281, 29.41624 61.26202, 29.4177 61.26, 29.42147 61.25971, 29.42466 61.25769, 29.43177 61.25843, 29.43627 61.25757, 29.43924 61.25653, 29.44216 61.25682, 29.44516 61.25851, 29.4486 61.25806, 29.45271 61.25728, 29.45838 61.25699, 29.46063 61.25875, 29.46765 61.25913, 29.47165 61.25785, 29.47558 61.25861, 29.48018 61.2574, 29.48269 61.25875, 29.48756 61.25707, 29.49976 61.25547, 29.51776 61.25129, 29.52138 61.24782, 29.52198 61.24208, 29.52902 61.24084, 29.53889 61.2432, 29.54507 61.23998, 29.54841 61.24241, 29.5552 61.24171, 29.55288 61.2337, 29.5582 61.22965, 29.56361 61.22688, 29.56687 61.22205, 29.56081 61.22178, 29.55899 61.21909, 29.55464 61.21741, 29.55281 61.21665, 29.56013 61.21355, 29.55916 61.21117, 29.56073 61.20948, 29.55878 61.20748, 29.56346 61.2026, 29.56426 61.20048, 29.57152 61.19695, 29.56898 61.19526, 29.56358 61.19375, 29.56346 61.19143, 29.5646 61.18861, 29.56448 61.1853, 29.56494 61.18224, 29.57135 61.18195, 29.57181 61.18718, 29.57684 61.19046, 29.58382 61.18962, 29.61984 61.18436, 29.62738 61.18073, 29.64403 61.18298, 29.65836 61.17921, 29.67462 61.17957, 29.68945 61.17495, 29.6924 61.17636, 29.70039 61.17508, 29.72997 61.17481, 29.75248 61.17418, 29.76443 61.17271, 29.78139 61.17247, 29.79347 61.17152, 29.84655 61.1703, 29.86004 61.17143, 29.87296 61.16827, 29.87737 61.16652, 29.88901 61.16548, 29.92401 61.16008, 29.95061 61.15663, 29.96941 61.153, 29.99846 61.14787, 30.04423 61.14531, 30.09767 61.14162, 30.1546 61.13535, 30.19663 61.13055, 31.00127 61.06024, 32.02707 60.83553, 32.87334 60.69032, 32.90714 60.68495, 32.91924 60.71351, 33.01112 60.72309, 33.06427 60.722, 33.15195 60.74092, 33.15322 60.76916, 33.17 60.78964, 33.19829 60.7905, 33.25502 60.79662, 33.2408 60.80437, 33.19799 60.80757, 33.18071 60.8142, 33.1755 60.82488, 33.20242 60.83122, 33.19047 60.83639, 33.19961 60.84333, 33.20349 60.85834, 33.21075 60.85631, 33.21713 60.85463, 33.21619 60.85011, 33.21882 60.84785, 33.22047 60.84605, 33.22405 60.84883, 33.2279 60.84503, 33.23085 60.84899, 33.23473 60.85197, 33.23461 60.8568, 33.25652 60.86758, 33.24757 60.87464, 33.2915 60.89011, 33.30247 60.88766, 33.32959 60.90146, 33.35053 60.90018, 33.3816 60.91172, 33.42632 60.91583, 33.44422 60.92672, 33.46432 60.93008, 33.46476 60.95227, 33.43923 60.96753, 33.4208 60.98423, 33.44781 61.00548, 33.50079 61.0057, 33.50669 61.00364, 33.51478 61.00594, 33.52226 61.00859, 33.54661 61.00659, 33.54882 60.98603, 33.5925 60.97624, 33.61056 60.96018, 33.63779 60.96032, 33.65278 60.95888, 33.69323 60.9585, 33.72307 60.94214, 33.74451 60.94263, 33.75901 60.94087, 33.7848 60.93949, 33.78533 60.94831, 33.81238 60.94921, 33.81165 60.9595, 33.8185 60.97297, 33.83068 60.97921, 33.83951 60.99181, 33.84908 61.00128, 33.85249 61.01267, 33.8748 61.05227, 33.89419 61.06077, 33.90663 61.07963, 33.90049 61.08971, 33.87271 61.09542, 33.83958 61.09031, 33.82028 61.10236, 33.79552 61.11302, 33.76803 61.11769, 33.74468 61.12533, 33.73437 61.11323, 33.70862 61.10519, 33.69162 61.10913, 33.67214 61.09318, 33.66926 61.07325, 33.64585 61.05896, 33.58471 61.0578, 33.53766 61.07714, 33.53199 61.09345, 33.51267 61.10492, 33.47211 61.11563, 33.43536 61.15612, 33.46229 61.16972, 33.49362 61.18976, 33.54567 61.22259, 33.60328 61.22974, 33.65612 61.22373, 33.66057 61.24121, 34.18052 61.2407, 34.18163 61.2487, 34.23575 61.25336, 34.25594 61.24447, 34.28114 61.23407, 34.33631 61.2249, 34.36665 61.19982, 34.41889 61.1631, 34.4148 61.15069, 34.44022 61.14448, 34.44744 61.16192, 34.46066 61.16732, 34.4659 61.18856, 34.485 61.20016, 34.47532 61.20563, 34.46224 61.20705, 34.47356 61.21933, 34.4696 61.23042, 34.47998 61.23894, 34.4915 61.23975, 34.50577 61.24716, 34.51497 61.25037, 34.52334 61.25974, 34.53851 61.26393, 34.54339 61.27768, 34.65682 61.27574, 34.72562 61.30019, 34.73159 61.32165, 34.77977 61.31845, 34.80112 61.29442, 34.87908 61.29362, 34.95171 61.27079, 35.05669 61.26927, 35.07896 61.2553, 35.10109 61.26681, 35.16708 61.24945, 35.19047 61.26041, 35.22001 61.26061, 35.23651 61.24926, 35.29648 61.25368, 35.32852 61.27124, 35.32498 61.28051, 35.35307 61.2891, 35.37021 61.28004, 35.39421 61.28648, 35.40068 61.27524, 35.37713 61.2409, 35.37978 61.22982, 35.36414 61.22491, 35.35742 61.17797, 35.40563 61.1771, 35.41436 61.17143, 35.40623 61.16413, 35.40214 61.14309, 35.41932 61.14483, 35.4492 61.16425, 35.47032 61.16137, 35.49118 61.16622, 35.49817 61.17229, 35.52261 61.18426, 35.55063 61.18297, 35.56395 61.18033, 35.5864 61.18621, 35.59934 61.18469, 35.60152 61.18028, 35.72592 61.21177, 35.71033 61.00376, 35.62975 60.95362, 35.52784 60.91353, 35.53319 60.89112, 35.49535 60.87891, 35.49459 60.86937, 35.47342 60.85721, 35.46832 60.85293, 35.42835 60.85263, 35.40966 60.85065, 35.34865 60.8548, 35.28568 60.83967, 35.2641 60.8422, 35.26656 60.83503, 35.26357 60.83294, 35.24599 60.83324, 35.2321 60.79807, 35.20661 60.79655, 35.20348 60.78507, 35.19481 60.78203, 35.18475 60.76894, 35.19324 60.7508, 35.22083 60.73569, 35.24698 60.7325, 35.24357 60.71323, 35.21508 60.70124, 35.23268 60.69706, 35.25903 60.6918, 35.26452 60.6885, 35.26075 60.68333, 35.26916 60.68191, 35.27182 60.67768, 35.265 60.66929, 35.26777 60.66636, 35.28649 60.63638, 35.28658 60.6175, 35.27774 60.60939, 35.30428 60.60257, 35.29558 60.47377, 35.28092 60.36167, 35.28653 60.33852, 35.28192 60.33561, 35.2617 60.33211, 35.26971 60.31999, 35.24736 60.28969, 35.19963 60.27172, 35.18065 60.25413, 35.18987 60.25274, 35.19772 60.2488, 35.19306 60.24476, 35.18781 60.24573, 35.1908 60.22581, 35.18279 60.2196, 35.19149 60.21317, 35.19223 60.20254, 35.18059 60.18723, 35.17579 60.17087, 35.18198 60.16747, 35.17065 60.15808, 35.18216 60.13253, 35.19615 60.12762, 35.20932 60.11507, 35.21866 60.06993, 35.16417 60.04698, 35.14654 60.04476, 35.14253 60.02084, 35.22979 60.01797, 35.28753 60.0175, 35.29003 59.99881, 35.3239 59.99942, 35.32882 59.98049, 35.4765 59.9827, 35.47911 59.92725, 35.42067 59.91895, 35.37978 59.86398, 35.41289 59.73055, 35.43151 59.71527, 35.47049 59.70608, 35.4996 59.70841, 35.51278 59.70404, 35.54707 59.71118, 35.55455 59.70831, 35.55447 59.70441, 35.56379 59.70319, 35.5671 59.70925, 35.58356 59.71304, 35.61239 59.72056, 35.6268 59.71752, 35.61202 59.61519, 35.50592 59.65337, 35.50369 59.54199, 35.4775 59.54277, 35.47437 59.54594, 35.45848 59.54369, 35.4417 59.53458, 35.43941 59.53426, 35.43955 59.53602, 35.43828 59.53741, 35.43632 59.53806, 35.43123 59.53633, 35.42914 59.53549, 35.42754 59.53485, 35.42751 59.5344, 35.42637 59.53426, 35.42448 59.53502, 35.42108 59.53714, 35.41612 59.53402, 35.41082 59.53103, 35.41503 59.52713, 35.42113 59.52362, 35.42456 59.52, 35.42644 59.51971, 35.43039 59.52133, 35.44652 59.51886, 35.45236 59.51989, 35.45493 59.5191, 35.45759 59.51964, 35.46677 59.52436, 35.47107 59.52299, 35.47235 59.51952, 35.47815 59.51704, 35.47947 59.51723, 35.48188 59.52008, 35.48919 59.52199, 35.4931 59.52112, 35.49625 59.51794, 35.49603 59.51208, 35.50087 59.50997, 35.50482 59.50848, 35.51101 59.50816, 35.5123 59.50557, 35.50974 59.50388, 35.464 59.50698, 35.42002 59.51627, 35.33415 59.52496, 35.31553 59.52261, 35.31326 59.49304, 35.34543 59.47842, 35.38231 59.47895, 35.38702 59.46771, 35.40173 59.45475, 35.4137 59.45087, 35.41073 59.44167, 35.39952 59.43246, 35.41381 59.43024, 35.41813 59.42756, 35.41876 59.42649, 35.41614 59.42401, 35.42141 59.42189, 35.42617 59.42719, 35.42882 59.42647, 35.43112 59.42829, 35.43656 59.42562, 35.44448 59.42557, 35.44348 59.42278, 35.43801 59.42295, 35.43904 59.42103, 35.43647 59.4199, 35.43261 59.41278, 35.43171 59.41018, 35.44963 59.4136, 35.45103 59.41089, 35.44805 59.40317, 35.42458 59.39755, 35.40189 59.38951, 35.3998 59.37936, 35.34829 59.36484, 35.32596 59.36423, 35.32098 59.36987, 35.3178 59.36079, 35.30913 59.35661, 35.30312 59.34878, 35.31221 59.33675, 35.35015 59.32856, 35.36748 59.33088, 35.37829 59.32891, 35.39872 59.33184, 35.39854 59.31971, 35.38648 59.31808, 35.37854 59.30453, 35.31243 59.30249, 35.25671 59.30007, 35.19796 59.28833, 35.19413 59.2787, 35.1989 59.27677, 35.17895 59.2615, 35.18265 59.25799, 35.15163 59.25606, 35.13141 59.2478, 35.10232 59.24517, 35.08233 59.24923, 35.04911 59.24802, 35.03838 59.25278, 35.02594 59.25034, 35.00585 59.24642, 34.99744 59.239, 34.97719 59.22378, 34.96552 59.2256, 34.94114 59.22089, 34.93076 59.21335, 34.92548 59.21156, 34.919 59.21126, 34.9132 59.21081, 34.90929 59.21283, 34.89631 59.21598, 34.8889 59.21016, 34.87615 59.21398, 34.86849 59.21003, 34.85455 59.20071, 34.8555 59.1701, 34.84513 59.17035, 34.83788 59.14963, 34.85192 59.15002, 34.84272 59.12671, 34.81396 59.12454, 34.79408 59.115, 34.78655 59.09815, 34.7639 59.08655, 34.75188 59.0915, 34.72631 59.08252, 34.71847 59.08713, 34.71857 59.09247, 34.72279 59.09692, 34.71688 59.09909, 34.70444 59.09773, 34.69077 59.10974, 34.67365 59.11294, 34.65739 59.12007, 34.62773 59.11232, 34.58 59.1205, 34.55355 59.11739, 34.52779 59.12861, 34.5089 59.14406, 34.47936 59.14042, 34.45048 59.15693, 34.42266 59.14177, 34.41918 59.15524, 34.39579 59.15956, 34.38951 59.18087, 34.3852 59.1816, 34.38346 59.17497, 34.37721 59.17754, 34.35442 59.19429, 34.36013 59.19978, 34.35028 59.19989, 34.34524 59.20352, 34.34174 59.19822, 34.33756 59.19574, 34.32307 59.19079, 34.33399 59.18374, 34.28361 59.17626, 34.25503 59.18796, 34.2467 59.20598, 34.21838 59.20474, 34.21374 59.17907, 34.20799 59.17045, 34.17615 59.17203, 34.15846 59.1814, 34.10808 59.1699, 34.10795 59.16678, 34.10651 59.161, 34.12156 59.15874, 34.12052 59.14193, 34.10713 59.13616, 34.09589 59.13745, 34.09357 59.14402, 34.07924 59.14002, 34.07452 59.13937, 34.0661 59.14989, 34.0316 59.14721, 34.01195 59.16142, 33.99402 59.16418, 33.98278 59.17109, 33.97394 59.17236, 33.96682 59.18471, 33.94632 59.18651, 33.94246 59.19684, 33.93538 59.19725, 33.93002 59.19942, 33.93578 59.21106, 33.93262 59.23411, 33.9256 59.23752, 33.92763 59.23088, 33.91747 59.22371, 33.91448 59.20569, 33.80312 59.19915, 33.79547 59.20792, 33.79195 59.21739, 33.79564 59.22124, 33.80138 59.22298, 33.80514 59.22853, 33.80994 59.22966, 33.81715 59.2307, 33.82332 59.22814, 33.82126 59.23102, 33.82022 59.23443, 33.80202 59.23843, 33.79411 59.24805, 33.79539 59.25007, 33.80492 59.24999, 33.80405 59.25754, 33.79613 59.26177, 33.78167 59.26356, 33.77564 59.27155, 33.78541 59.27534, 33.77678 59.28274, 33.75855 59.27759, 33.75267 59.28122, 33.76688 59.28765, 33.76047 59.29075, 33.74789 59.28754, 33.74114 59.29239, 33.73508 59.29689, 33.7065 59.29853, 33.69097 59.31068, 33.66948 59.30782, 33.66308 59.30882, 33.66678 59.31589, 33.67185 59.3219, 33.66482 59.32763, 33.65728 59.32513, 33.61987 59.32648, 33.63301 59.38138, 33.54337 59.38607, 33.52936 59.37696, 33.48201 59.37862, 33.45424 59.37538, 33.3559 59.38367, 33.34407 59.3125, 33.25991 59.31996, 33.26076 59.33716, 33.2465 59.34665, 33.23585 59.36194, 33.21488 59.36261, 33.15974 59.38299, 33.15112 59.37433, 33.14693 59.37539, 33.14165 59.36981, 33.11519 59.38775, 33.06985 59.37928, 33.06551 59.38272, 33.07382 59.38462, 33.06471 59.38841, 33.06658 59.39002, 33.06227 59.41819, 33.05485 59.42228, 33.04504 59.42112, 33.02725 59.4257, 33.01132 59.42546, 32.99573 59.42399, 32.99048 59.42402, 32.96841 59.41864, 32.97089 59.40687, 32.97096 59.39509, 32.97764 59.3925, 32.95891 59.38012, 32.94293 59.37264, 32.9248 59.3466, 32.84894 59.347, 32.77927 59.30221, 32.76488 59.28548, 32.75528 59.28448, 32.74843 59.2798, 32.73867 59.27643, 32.73498 59.27177, 32.74018 59.26943, 32.74467 59.26739, 32.74641 59.26464, 32.74366 59.26221, 32.73972 59.26328, 32.73551 59.2618, 32.73358 59.25948, 32.73131 59.25909, 32.72994 59.25753, 32.72651 59.25704, 32.73321 59.25544, 32.73669 59.25272, 32.74085 59.25175, 32.74643 59.24823, 32.7382 59.2396, 32.74714 59.23392, 32.74973 59.23056, 32.75617 59.23045, 32.76192 59.22842, 32.76837 59.22654, 32.77245 59.22069, 32.77138 59.21711, 32.76495 59.21291, 32.77101 59.21081, 32.76711 59.20607, 32.75768 59.20451, 32.75822 59.2012, 32.74974 59.20017, 32.74915 59.1972, 32.7394 59.19567, 32.74227 59.19396, 32.75166 59.19437, 32.76666 59.18831, 32.76199 59.18421, 32.7611 59.17975, 32.74712 59.17359, 32.73365 59.17175, 32.72911 59.1479, 32.72033 59.1468, 32.71361 59.14323, 32.68286 59.14406, 32.67203 59.14136, 32.67507 59.13968, 32.66595 59.12879, 32.64996 59.11615, 32.62231 59.11222, 32.61576 59.10859, 32.56352 59.11472, 32.57852 59.1358, 32.50392 59.11107, 32.44836 59.1059, 32.44341 59.11389, 32.44739 59.11694, 32.43906 59.11581, 32.42832 59.11573, 32.41818 59.1228, 32.42468 59.12426, 32.41914 59.12999, 32.41016 59.12814, 32.39844 59.13193, 32.38588 59.14146, 32.40731 59.1524, 32.43381 59.15223, 32.45771 59.14246, 32.47474 59.13903, 32.47394 59.15073, 32.42198 59.15818, 32.41981 59.16053, 32.40652 59.16, 32.38705 59.16298, 32.38313 59.16755, 32.3719 59.17, 32.36033 59.17105, 32.34851 59.17174, 32.33043 59.17838, 32.31899 59.18029, 32.30297 59.18722, 32.30251 59.19491, 32.28185 59.20007, 32.2765 59.21232, 32.2852 59.2164, 32.29871 59.21662, 32.3017 59.22092, 32.28855 59.22013, 32.28501 59.22426, 32.30162 59.23462, 32.31027 59.23383, 32.31425 59.24116, 32.31274 59.24638, 32.29039 59.24226, 32.27764 59.25954, 32.31777 59.26156, 32.31739 59.26358, 32.29218 59.26234, 32.29236 59.26617, 32.29804 59.27, 32.29566 59.27835, 32.27511 59.28769, 32.25323 59.28707, 32.25033 59.28934, 32.22683 59.27793, 32.21828 59.27371, 32.18415 59.31096, 32.19781 59.31529, 32.21144 59.32014, 32.21546 59.32718, 32.21978 59.32901, 32.22178 59.33019, 32.22377 59.33268, 32.22839 59.33981, 32.22923 59.3437, 32.22939 59.34802, 32.22795 59.35232, 32.22575 59.35505, 32.22072 59.35501, 32.22602 59.34865, 32.22193 59.34637, 32.22469 59.33927, 32.21697 59.33301, 32.18882 59.31836, 32.16222 59.32937, 32.16482 59.33317, 32.14271 59.35028, 32.11221 59.37222, 32.11714 59.3811, 32.10491 59.38246, 32.0961 59.38033, 32.08274 59.38425, 32.07076 59.39132, 32.03442 59.40337, 32.03317 59.38585, 32.02346 59.39419, 32.01444 59.40043, 32.00738 59.40383, 32.00116 59.40886, 31.99411 59.40918, 31.98706 59.40984, 31.98394 59.40838, 31.98458 59.40649, 31.97486 59.40377, 31.96915 59.40636, 31.98945 59.42201, 31.98724 59.42914, 31.98435 59.43381, 31.9686 59.43191, 31.96124 59.42781, 31.94074 59.41747, 31.92963 59.41064, 31.92253 59.40224, 31.92276 59.39717, 31.91371 59.39454, 31.91859 59.39089, 31.9147 59.38732, 31.91348 59.3831, 31.91192 59.37696, 31.90497 59.37266, 31.8897 59.36896, 31.87257 59.37655, 31.87945 59.36933, 31.8884 59.36736, 31.8693 59.36501, 31.85982 59.35952, 31.83536 59.35763, 31.82695 59.35281, 31.80602 59.35226, 31.80671 59.34314, 31.79092 59.33858, 31.8105 59.32998, 31.82458 59.32875, 31.82528 59.3246, 31.81739 59.32393, 31.80951 59.3201, 31.80268 59.31251, 31.7943 59.31212, 31.79141 59.31471, 31.77672 59.31357, 31.77318 59.31594, 31.76312 59.31497, 31.76016 59.31883, 31.75377 59.32006, 31.74161 59.3143, 31.72345 59.32239, 31.73277 59.32523, 31.74395 59.32858, 31.75658 59.33322, 31.75465 59.33432, 31.7589 59.33892, 31.74772 59.34147, 31.7362 59.34007, 31.73444 59.33554, 31.70895 59.33504, 31.699 59.34389, 31.72304 59.34382, 31.73334 59.34383, 31.73403 59.3476, 31.70502 59.34734, 31.68562 59.35969, 31.65027 59.36058, 31.60839 59.3746, 31.58111 59.36999, 31.55382 59.36819, 31.54526 59.35665, 31.55043 59.35317, 31.55185 59.33219, 31.54366 59.32696, 31.54439 59.31683, 31.55821 59.31181, 31.55723 59.30597, 31.57235 59.29867, 31.57047 59.295, 31.56378 59.29414, 31.54473 59.29275, 31.54036 59.28382, 31.5598 59.25233, 31.58429 59.23271, 31.57284 59.22171, 31.57101 59.21142, 31.55167 59.20824, 31.53576 59.19926, 31.51293 59.20965, 31.50585 59.20627, 31.51525 59.20078, 31.47727 59.16715, 31.49536 59.16722, 31.49205 59.13874, 31.47391 59.12413, 31.47995 59.11471, 31.47679 59.11036, 31.48501 59.10228, 31.47366 59.0935, 31.46333 59.08634, 31.42745 59.06814, 31.39098 59.06187, 31.37789 59.05255, 31.3775 59.02715, 31.35827 59.02348, 31.33903 59.02264, 31.33078 59.02604, 31.31841 59.02378, 31.32526 59.01786, 31.30876 59.01193, 31.3073 59.00519, 31.21787 59.00479, 31.18612 59.04494, 31.16544 59.04523, 31.15162 59.05399, 31.16199 59.06434, 31.15999 59.07399, 31.12717 59.08763, 31.08623 59.08879, 31.06795 59.06915, 31.05377 59.07111, 31.04988 59.07591, 31.0254 59.0664, 30.99474 59.03889, 30.98871 59.04095, 30.98406 59.04373, 30.97424 59.04291, 30.97296 59.03316, 30.95942 59.03237, 30.95844 59.01738, 30.97428 59.01634, 31.01072 59.00009, 30.98472 58.98935, 30.98442 58.97973, 30.95872 58.97294, 30.92064 58.90899, 30.87614 58.89654, 30.82614 58.87415, 30.78383 58.87053, 30.76513 58.85478, 30.7272 58.84967, 30.68927 58.85308, 30.68842 58.84407, 30.73426 58.82084, 30.72242 58.80754, 30.71375 58.75961, 30.71021 58.75047, 30.72041 58.7456, 30.71687 58.72932, 30.7074 58.72543, 30.6993 58.7194, 30.68312 58.71197, 30.66573 58.72108, 30.67306 58.73019, 30.66576 58.73771, 30.6406 58.72706, 30.62321 58.71495, 30.60216 58.71318, 30.59347 58.70285, 30.57111 58.70228, 30.57416 58.71099, 30.56073 58.72397, 30.5322 58.72091, 30.49679 58.72142, 30.48886 58.71444, 30.45468 58.70559, 30.44659 58.69889, 30.40578 58.701, 30.38743 58.71418, 30.36497 58.7188, 30.37145 58.73894, 30.36146 58.74552, 30.34285 58.74516, 30.33858 58.75654, 30.32992 58.75262, 30.32676 58.74299, 30.31535 58.74085, 30.28335 58.73567, 30.28055 58.74311, 30.27157 58.74699, 30.24538 58.74137, 30.24133 58.7428, 30.23144 58.74226, 30.22555 58.74511, 30.22817 58.74939, 30.21967 58.75081, 30.2208 58.75811, 30.21574 58.75828, 30.21274 58.75008, 30.20356 58.74794, 30.19756 58.74224, 30.18615 58.74695, 30.18298 58.75594, 30.17802 58.74756, 30.15435 58.74221, 30.12912 58.74403, 30.13298 58.7371, 30.12998 58.73089, 30.12209 58.71885, 30.10494 58.72401, 30.09877 58.72026, 30.1156 58.7084, 30.11869 58.70153, 30.14208 58.69366, 30.11591 58.67246, 30.08973 58.66767, 30.07171 58.66453, 30.06537 58.65568, 30.08279 58.65447, 30.11945 58.64111, 30.09595 58.63482, 30.09717 58.62781, 30.0804 58.62595, 30.10726 58.60364, 30.12255 58.57544, 30.12676 58.56671, 30.14191 58.56055, 30.14811 58.55102, 30.16426 58.53945, 30.17903 58.53361, 30.16192 58.52284, 30.15199 58.52605, 30.12833 58.52067, 30.11745 58.51346, 30.10726 58.51235, 30.10256 58.50478, 30.122 58.49861, 30.13732 58.49172, 30.12344 58.48689, 30.11128 58.4878, 30.10034 58.4794, 30.07859 58.47718, 30.06422 58.47398, 30.0626 58.4647, 30.07037 58.4624, 30.06559 58.45779, 30.07385 58.44671, 30.06061 58.44291, 30.04532 58.44018, 30.02228 58.44443, 30.00778 58.43641, 29.95209 58.4344, 29.90395 58.43203, 29.86478 58.42555, 29.78991 58.42033, 29.77749 58.41564, 29.75752 58.42048, 29.74441 58.42136, 29.74264 58.42994, 29.74705 58.43565, 29.74418 58.4395, 29.72198 58.44003, 29.70916 58.44287, 29.70343 58.45664, 29.69702 58.45927, 29.68219 58.45113, 29.68336 58.44723, 29.67629 58.44226, 29.66096 58.44701, 29.65592 58.44494, 29.63692 58.44582, 29.62502 58.45153, 29.6122 58.45037, 29.60613 58.45827, 29.59595 58.4579, 29.58656 58.4683, 29.5932 58.46862, 29.58106 58.475, 29.56501 58.47405, 29.54485 58.48028, 29.53472 58.49344, 29.54339 58.50146, 29.54657 58.51449, 29.56829 58.52, 29.58176 58.52765, 29.54662 58.54187, 29.50845 58.53966, 29.48264 58.54892, 29.47347 58.5641, 29.44575 58.57389, 29.42353 58.58584, 29.40999 58.57536, 29.40057 58.55198, 29.3567 58.55302, 29.3276 58.56096, 29.31957 58.56761, 29.32399 58.57185, 29.31845 58.57644, 29.33655 58.5814, 29.35155 58.57847, 29.36244 58.58387, 29.36662 58.58406, 29.37184 58.59015, 29.38014 58.5864, 29.38729 58.58694, 29.38964 58.58835, 29.39044 58.59128, 29.39519 58.59305, 29.38998 58.59536, 29.37319 58.60111, 29.36189 58.60239, 29.35085 58.60636, 29.34735 58.60962, 29.34792 58.60683, 29.34047 58.60841, 29.33113 58.61151, 29.31815 58.61184, 29.32302 58.60931, 29.31904 58.60567, 29.29242 58.61116, 29.28758 58.59925, 29.27258 58.59448, 29.25522 58.59764, 29.24817 58.61189, 29.24298 58.62536, 29.22781 58.62833, 29.23042 58.63321, 29.24779 58.63452, 29.26438 58.63701, 29.26999 58.64485, 29.28669 58.64875, 29.26518 58.6574, 29.26426 58.66265, 29.2638 58.67085, 29.26677 58.67941, 29.26803 58.6894, 29.28165 58.69688, 29.25911 58.69922, 29.2462 58.70333, 29.22744 58.70638, 29.2183 58.69623, 29.20276 58.7, 29.20014 58.70581, 29.18653 58.70876, 29.17872 58.70184, 29.17041 58.70224, 29.16348 58.70515, 29.15861 58.70092, 29.15168 58.69955, 29.13742 58.69889, 29.11777 58.71158, 29.10497 58.71713, 29.09081 58.72357, 29.08317 58.73109, 29.07398 58.73094, 29.05724 58.72366, 29.03741 58.72458, 29.03406 58.72763, 29.03841 58.72863, 29.04104 58.72776, 29.04788 58.73024, 29.05284 58.72929, 29.0718 58.73605, 29.06655 58.73904, 29.05582 58.73883, 29.05023 58.75518, 29.04744 58.76531, 29.0419 58.7701, 29.04599 58.77726, 29.03704 58.82568, 29.02405 58.82483, 29.0241 58.81439, 29.01179 58.81212, 29.00361 58.80879, 28.98803 58.81146, 28.96648 58.79687, 28.90346 58.80221, 28.88432 58.81625, 28.84045 58.8246, 28.80482 58.82638, 28.79941 58.8246, 28.77052 58.83064, 28.77221 58.8269, 28.77184 58.82423, 28.76115 58.82442, 28.74428 58.82853, 28.73939 58.83247, 28.66849 58.85066, 28.66317 58.85037, 28.65922 58.85221, 28.66574 58.85731, 28.66763 58.86342, 28.66677 58.86597, 28.66041 58.86484, 28.65818 58.85554, 28.64444 58.85221, 28.64018 58.85471, 28.63523 58.85456, 28.63185 58.86028, 28.61994 58.86268, 28.61947 58.8648, 28.60009 58.86782, 28.59822 58.86728, 28.60101 58.86444, 28.58882 58.8598, 28.58543 58.86273, 28.58751 58.86416, 28.58582 58.86593, 28.57595 58.8663, 28.56865 58.86533, 28.56393 58.86747, 28.55784 58.86573, 28.5521 58.86559, 28.54182 58.86681, 28.54406 58.86289, 28.53395 58.86004, 28.52623 58.86251, 28.52264 58.8657, 28.51734 58.86363, 28.51306 58.86583, 28.5045 58.86199, 28.49731 58.86117, 28.49223 58.86177, 28.4892 58.86379, 28.4859 58.86837, 28.47725 58.86883, 28.47562 58.87123, 28.47194 58.87222, 28.45344 58.87794, 28.44765 58.87612, 28.4372 58.87894, 28.43575 58.87458, 28.43399 58.87285, 28.43071 58.87269, 28.42564 58.87066, 28.42554 58.86791, 28.4182 58.86494, 28.41625 58.86185, 28.41321 58.86049, 28.41101 58.85821, 28.4167 58.85619, 28.41596 58.85459, 28.40974 58.85353, 28.3863 58.86728, 28.38694 58.87114, 28.37834 58.87254, 28.37248 58.87571, 28.36212 58.87013, 28.36688 58.8681, 28.3599 58.86013, 28.34949 58.86139, 28.34526 58.85378, 28.32822 58.85107, 28.32423 58.84605, 28.32873 58.8442, 28.34079 58.83312, 28.35083 58.82801, 28.34598 58.82344, 28.31224 58.83839, 28.3125 58.83041, 28.29996 58.82316, 28.2945 58.82528, 28.2916 58.83007, 28.28672 58.83868, 28.28321 58.83966, 28.25995 58.8739, 28.242 58.87396, 28.23767 58.8802, 28.24521 58.88177, 28.25343 58.88227, 28.26046 58.88357, 28.26525 58.88567, 28.26464 58.88954, 28.25801 58.89119, 28.25644 58.89698, 28.24111 58.895, 28.22749 58.89443, 28.21532 58.89726, 28.20727 58.90435, 28.21177 58.9171, 28.2043 58.92133, 28.20723 58.92505, 28.19369 58.93727, 28.19819 58.94044, 28.18384 58.93971, 28.14294 58.96228, 28.16781 58.97303, 28.15003 58.97098, 28.14049 58.97529, 28.11881 58.97201, 28.11602 58.99103, 28.09812 58.99164, 28.09535 58.9969, 28.08641 58.9921, 28.07609 58.99013, 28.05684 58.99044, 28.04791 58.98493, 28.03347 58.98579, 28.02947 58.99198, 28.02547 58.99959, 27.99136 59.00418, 27.99045 59.01266, 27.93186 59.01335, 27.91997 59.019, 27.88712 59.01449, 27.82751 58.99936, 27.73642 58.97653, 27.74283 58.99018, 27.73779 59.00796, 27.741 59.03069, 27.76618 59.03463, 27.7687 59.05376, 27.78496 59.07218, 27.78543 59.09428, 27.80215 59.10832, 27.80513 59.12941, 27.85549 59.15875, 27.88663 59.18596, 27.89224 59.22023, 27.89923 59.23902, 27.92406 59.25709, 27.95165 59.26954, 27.96593 59.27721, 27.98707 59.27647, 28.04446 59.2911, 28.08538 59.29312, 28.12629 59.29162, 28.14455 59.30888, 28.15731 59.32614, 28.16747 59.33703, 28.18724 59.34443, 28.19465 59.35812, 28.20755 59.37041, 28.20037 59.37629, 28.20418 59.38217, 28.19806 59.39532, 28.17483 59.40553, 28.13662 59.41897, 28.11682 59.44018, 28.09016 59.453, 28.05801 59.46302, 28.04262 59.46524, 28.03547 59.48106, 27.72716 59.99762, 27.699 60.33983))', 4326)
		, tt.point
	) AS {nameof(OsmAddress.IsLen)},
	ST_Y(tt.point) AS {nameof(OsmAddress.Latitude)},
	ST_X(tt.point) AS {nameof(OsmAddress.Longitude)}
FROM
	(
	SELECT
		t.city,
		t.street,
		t.house,		
		ST_Centroid(ST_Transform(t.geom, 4326)) AS point
	FROM (
		SELECT
			tags->'addr:city' AS city,
			CASE
				WHEN tags->'addr:street' IS NULL THEN tags->'addr:place'
				ELSE tags->'addr:street' 
			END AS street,
			tags->'addr:housenumber' AS house,
			pop.way AS geom
		FROM 
			public.planet_osm_polygon pop 
		WHERE
			tags->'addr:housenumber' IS NOT NULL
			AND tags->'addr:city'IS NOT NULL
			AND (tags->'addr:place' IS NOT NULL OR
			tags->'addr:street' IS NOT NULL)
		LIMIT {batch} OFFSET {offset}
	) t
) tt
;";

				var result = session.CreateSQLQuery(sql)
					.SetResultTransformer(Transformers.AliasToBean<OsmAddress>())
					.List<OsmAddress>();
				offset += batch;
				return result;
			}
		}
	}

	public class OsmAddress
	{
		public string City { get; set; }
		public string Street { get; set; }
		public string House { get; set; }
		public bool IsLen { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
	}
	/*
	public class OsmAddressMap : ClassMap<OsmAddress>
	{
		public OsmAddressMap()
		{
			Map(x => x.Geometry).Column("").CustomType<NHibernate.Spatial.Type.GeometryType>()
		}
	}*/
}
