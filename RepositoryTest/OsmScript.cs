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
using FluentNHibernate.Mapping;
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

namespace RepositoryTest
{
	public class OsmScript : IDisposable
	{
		SshClient _sshClient;
		private ISessionFactory _fiasSessionFactory;
		private ISessionFactory _osmSessionFactory;

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
			//ConfigureOsmDatabaseConnection();
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

			//var mapAssembly = Assembly.GetAssembly(typeof(Fias.Domain.AssemblyFinder));

			/*fluenConfig.Mappings(m =>
			{
				m.FluentMappings.AddFromAssembly(mapAssembly);
			});*/
			_fiasSessionFactory = fluenConfig.BuildSessionFactory();
		}

		/*
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
		}*/

		public void Start()
		{
			StreetRepository repository = new StreetRepository(_fiasSessionFactory);
			var streets = repository.GetStreets("Садовая", new Guid("c2deb16a-0330-4f05-821f-1d09c93331e6"));
		}
	}
}
