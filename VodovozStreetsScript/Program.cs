using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Dialect;
using Npgsql;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.IO;
using System.Net;

namespace VodovozStreetsScript
{
    class Program
    {
		static SshClient _sshClient;
		static ISessionFactory _fiasSessionFactory;
		static ISessionFactory _vodovozSessionFactory;

		private static string _vodovozLogin;
		private static string _vodovozPassword;
		private static string _dadataApiToken;
		private static string _dadataSecretToken;
		private static int _idFrom;
		private static int _idTo;

		static void Main(string[] args)
        {
			var keyFile = Path.Combine(Environment.CurrentDirectory, "key");
			if(!File.Exists(keyFile))
			{
				Console.WriteLine("Не найден ssh ключ в каталоге с программой");
				return;
			}

			PrivateKeyFile privateKeyFile = new PrivateKeyFile(keyFile, "7!86-B4.6B-49A-976-43dBA");
			ConnectionInfo connectionInfo = new PrivateKeyConnectionInfo("srv2.vod.qsolution.ru", 2208, "root", privateKeyFile);
			connectionInfo.Timeout = TimeSpan.FromSeconds(30);

			_sshClient = new SshClient(connectionInfo);

			try
			{
				Console.Write("Trying SSH connection... ");
				_sshClient.Connect();
				if(_sshClient.IsConnected)
				{
					Console.WriteLine("Connected.");
				}
				else
				{
					Console.WriteLine("SSH connection has failed");
					Console.ReadKey();
					return;
				}

				Console.Write("Trying forwarding port 5433 -> 5432... ");
				var portFwld = new ForwardedPortLocal(IPAddress.Loopback.ToString(), 5433, IPAddress.Loopback.ToString(), 5432);
				_sshClient.AddForwardedPort(portFwld);
				portFwld.Start();
				if(portFwld.IsStarted)
				{
					Console.WriteLine("Port forwarded");
				}
				else
				{
					Console.WriteLine("Port forwarding has failed.");
					Console.ReadKey();
					return;
				}
			}
			catch(SshException e)
			{
				Console.WriteLine($"SSH client connection error: \n{e.Message}");
				Console.ReadKey();
				return;
			}
			catch(System.Net.Sockets.SocketException e)
			{
				Console.WriteLine($"Socket connection error: \n{e.Message}");
				Console.ReadKey();
				return;
			}

			var configs = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "config.txt"));
			_dadataApiToken = configs[0];
			_dadataSecretToken = configs[1];
			_vodovozLogin = configs[2];
			_vodovozPassword = configs[3];
			_idFrom = int.Parse(configs[4]);
			_idTo = int.Parse(configs[5]);

			if(!ConfigureFiasDatabaseConnection())
			{
				return;
			}
			if(!ConfigureVodovozDatabaseConnection())
			{
				return;
			}

			var geocoder = new GeoCoderScript(_fiasSessionFactory, _vodovozSessionFactory, _dadataApiToken, _dadataSecretToken, _idFrom, _idTo);
			geocoder.Start();

			Console.ReadKey();
		}

		private static bool ConfigureFiasDatabaseConnection()
		{
			Console.Write("Настройка подключения к базе Fias. ");
			try
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
					;

				var fluenConfig = Fluently.Configure().Database(databaseConfig);

				_fiasSessionFactory = fluenConfig.BuildSessionFactory();
				Console.WriteLine("Успешно.");
			}
			catch(Exception ex)
			{
				Console.WriteLine();
				Console.WriteLine("Ошибка настройки подключения к бд. ", ex.Message);
				return false;
			}

			return true;
		}

		private static bool ConfigureVodovozDatabaseConnection()
		{
			Console.Write("Настройка подключения к базе Vodovoz. ");
			try
			{
				var connectionBuilder = new MySqlConnectionStringBuilder();
				connectionBuilder.Server = "sql.vod.qsolution.ru";
				connectionBuilder.Port = 3306;
				connectionBuilder.Database = "Vodovoz_honeybee";
				connectionBuilder.UserID = _vodovozLogin;
				connectionBuilder.Password = _vodovozPassword;
				connectionBuilder.SslMode = MySqlSslMode.None;

				var databaseConfig = MySQLConfiguration.Standard
					.Dialect<MySQL57Dialect>()
					.ConnectionString(connectionBuilder.GetConnectionString(true))
					.AdoNetBatchSize(100);

				var fluenConfig = Fluently.Configure().Database(databaseConfig);

				_vodovozSessionFactory = fluenConfig.BuildSessionFactory();
			}
			catch(Exception ex)
			{
				Console.WriteLine();
				Console.WriteLine("Ошибка настройки подключения к бд. ", ex.Message);
				return false;
			}
			
			Console.WriteLine("Успешно.");
			return true;
		}
	}
}
