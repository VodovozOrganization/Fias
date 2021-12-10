using ConsoleTest;
using Fias.LoadModel;
using Fias.Search;
using Fias.Source;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Configuration;
using NHibernate;
using NHibernate.Dialect;
using NLog;
using Npgsql;
using System;
using System.IO;
using System.Reflection;

namespace Domain
{
	class Program
	{
		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private static ISessionFactory _sessionFactory;

		private static string _host;
		private static int _port;
		private static string _database;
		private static string _user;
		private static string _password;

		static void Main(string[] args)
		{
			//ConfigureDatabaseConnection();

			OsmScript osmScript = new OsmScript();

			osmScript.StartAsync();



			/*
			try
			{
				var configFile = Path.Combine(Environment.CurrentDirectory, "config.ini");

				var builder = new ConfigurationBuilder()
					.AddIniFile(configFile, optional: false);

				var configuration = builder.Build();

				var mysqlSection = configuration.GetSection("Postgresql");
				_host = mysqlSection["host"];
				_port = int.Parse(mysqlSection["port"]);
				_database = mysqlSection["database"];
				_user = mysqlSection["user"];
				_password = mysqlSection["password"];
			}
			catch(Exception ex)
			{
				_logger.Fatal(ex, "Ошибка чтения конфигурационного файла.");
				return;
			}

			ConfigureDatabaseConnection();

			Loader loader = new Loader(_sessionFactory);
			loader.LoadFromFiasServer();
			*/

			//var garPath = @"C:\Users\Enzo\Downloads\gar_xml.zip";
			//loader.LoadFromFile(garPath);

			/*
			Console.WriteLine($"Gar file size: {GetFileSize(new Uri("https://fias-file.nalog.ru/downloads/2021.11.23/gar_xml.zip"))}");

			CheckAvailableSpace();

			*/

			/*using(var session = _sessionFactory.OpenSession())
			{
				var regions = session.QueryOver<Region>().List();
			}*/

			/*var path = @"C:\Users\Enzo\source\repos\PostgreSqlTest\Fias\DebugSchemas\AS_ADDR_OBJ_2_251_01_04_01_01.xsd";
			XmlSchema schema = null;
			using(FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				schema = XmlSchema.Read(stream, ValidationHandler);

			}

			var levelsPath = @"C:\Users\Enzo\Desktop\fias_LEN\78\AS_ADDR_OBJ_20211011_2c4215c5-6772-4620-b361-26ec6000aeb8.XML";
			
			using(FileStream stream = new FileStream(levelsPath, FileMode.Open, FileAccess.Read))
			using(var levelReader = new ElementReader<AddressObject>(stream, "OBJECT", schema))
			{
				Stopwatch s = new Stopwatch();
				s.Start();
				while(levelReader.CanReadNext)
				{
					var level = levelReader.ReadNext();
				}
				s.Stop();
				Console.WriteLine($"{s.Elapsed.TotalSeconds}");
				//var levels = levelReader.ReadAll().ToList();
			}*/




			//var garPath = @"C:\Users\Enzo\Downloads\gar_xml.zip";
			/*var garPath = @"C:\Users\Enzo\Downloads\gar_delta_xml_12102021.zip";
			
			var fileStream = new FileStream(garPath, FileMode.Open, FileAccess.Read);
			var garFile = new ZipFile(fileStream);
			FiasReaderFactory readerFactory = new FiasReaderFactory(garFile);*/
			/*Load<FiasObjectLevel>(readerFactory);
			Load<FiasApartmentType>(readerFactory);
			Load<FiasHouseType>(readerFactory);
			Load<FiasOperationType>(readerFactory);
			Load<FiasAddressObjectType>(readerFactory);
			Load<FiasParameterType>(readerFactory);
			Load<FiasAddressObject>(readerFactory, 78);
			Load<FiasHouse>(readerFactory, 78);
			Load<FiasStead>(readerFactory, 78);
			Load<FiasApartment>(readerFactory, 78);
			Load<FiasAdmHierarchy>(readerFactory, 78);
			Load<FiasMunHierarchy>(readerFactory, 78);
			Load<FiasObjectDivision>(readerFactory, 78);
			Load<FiasAddressObjectParameter>(readerFactory, 78);
			Load<FiasHouseParameter>(readerFactory, 78);
			Load<FiasSteadParameter>(readerFactory, 78);
			Load<FiasApartmentParameter>(readerFactory, 78);*/

			//LevelRepository levelRepository = new LevelRepository(_sessionFactory);


			/*
			CityRepository repository = new CityRepository(_sessionFactory);
			var sds = repository.GetCities(10);
			var sss = repository.GetCity(new Guid("c2deb16a-0330-4f05-821f-1d09c93331e6"));
			var yyy = repository.GetCities("санкт");


			StreetRepository streetRepository = new StreetRepository(_sessionFactory);
			var streets1 = streetRepository.GetStreets(new Guid("c2deb16a-0330-4f05-821f-1d09c93331e6"), 10);
			var streets2 = streetRepository.GetStreets("ново", new Guid("c2deb16a-0330-4f05-821f-1d09c93331e6"));
			var street = streetRepository.GetStreet(new Guid("bb8d9f4d-5f25-4770-a185-957afb309166"));
			*/

			/*
			ApartmentRepository apartmentRepository = new ApartmentRepository(_sessionFactory);
			var apart = apartmentRepository.GetApartment(new Guid("00c47588-efbb-432a-95de-9604bb87d1f3"));
			var aparts1 = apartmentRepository.GetApartments(new Guid("2e48b311-b2d0-4ef4-8898-876750e11ade"));
			var aparts2 = apartmentRepository.GetApartments("1", new Guid("2e48b311-b2d0-4ef4-8898-876750e11ade"));
			*/

			


			/*
			LevelModel levelModel = new LevelModel(readerFactory, _sessionFactory, entityFactory);
			levelModel.LoadLevels();
			
			AddressTypeModel addressTypeModel = new AddressTypeModel(levelModel, readerFactory, _sessionFactory);
			addressTypeModel.LoadAndUpdateAddressObjectTypes();*/
			/*
			HouseTypeModel houseTypeModel = new HouseTypeModel(readerFactory, _sessionFactory);
			houseTypeModel.LoadAndUpdateHouseTypes();			

			ApartmentTypeModel apartmentTypeModel = new ApartmentTypeModel(readerFactory, _sessionFactory);
			apartmentTypeModel.LoadAndUpdateApartmentTypes();

			AddressModel addressModel = new AddressModel(addressTypeModel, levelModel, readerFactory, _sessionFactory);
			addressModel.LoadAndUpdateAddressObjects(47);

			SteadModel steadModel = new SteadModel(readerFactory, _sessionFactory);
			steadModel.LoadAndUpdateSteads(47);

			HouseModel houseModel = new HouseModel(houseTypeModel, readerFactory, _sessionFactory);
			houseModel.LoadAndUpdateHouses(47);

			ApartmentModel apartmentModel = new ApartmentModel(apartmentTypeModel, readerFactory, _sessionFactory);
			apartmentModel.LoadAndUpdateApartments(47);

			ReestrObjectModel reestrObjectModel = new ReestrObjectModel(levelModel, readerFactory, _sessionFactory);
			reestrObjectModel.LoadAndUpdateReestrObjects(47);
			*/
			/*HierarchyModel hierarchyModel = new HierarchyModel(readerFactory, _sessionFactory);
			hierarchyModel.LoadAndUpdateHierarchy(47);*/
		}

		//private static void ConfigureDatabaseConnection()
		//{
		//	/*var connectionBuilder = new NpgsqlConnectionStringBuilder();
		//	connectionBuilder.Host = _host;
		//	connectionBuilder.Port = _port;
		//	connectionBuilder.Database = _database;
		//	connectionBuilder.Username = _user;
		//	connectionBuilder.Password = _password;
		//	connectionBuilder.SslMode = SslMode.Disable;*/

		//	/*
		//	connectionBuilder.Host = "localhost";
		//	connectionBuilder.Port = 5432;
		//	connectionBuilder.Database = "test";
		//	connectionBuilder.Username = "postgres";
		//	connectionBuilder.Password = "ihoom1";
		//	connectionBuilder.SslMode = SslMode.Disable;
		//	 */

		//	var databaseConfig = PostgreSQLConfiguration.Standard
		//		.Dialect<PostgreSQL83Dialect>()
		//		.AdoNetBatchSize(100)
		//		.ConnectionString(connectionBuilder.ConnectionString)
		//		//.Driver<LoggedNpgsqlDriver>()
		//		;

		//	var fluenConfig = Fluently.Configure().Database(databaseConfig);

		//	var mapAssembly = Assembly.GetAssembly(typeof(Fias.Domain.AssemblyFinder));

		//	fluenConfig.Mappings(m => {
		//		m.FluentMappings.AddFromAssembly(mapAssembly);
		//	});
		//	_sessionFactory = fluenConfig.BuildSessionFactory();

		//}
	}
}
