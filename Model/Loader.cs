using Fias.Source;
using ICSharpCode.SharpZipLib.Zip;
using NHibernate;
using System;
using System.IO;
using System.Net;

namespace Fias.LoadModel
{
	public class Loader
	{
		private readonly ISessionFactory _sessionFactory;

		public Loader(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
		}

		public void LoadFromFiasServer() 
		{
			/*string url = GetLastGarUrl(10);
			if(!CheckAvailableSpace(new Uri(url)))
			{
				return;
			}

			CreateTempDirectory();
			try
			{
				var tempFile = GetTempFilePath();
				var loaded = DownloadGarArchive(url, tempFile);
				if(!loaded)
				{
					Console.WriteLine("Архив ФИАС не был загружен");
					return;
				}

			}
			finally
			{
				ClearTemp();
			}*/

			LoadFromFile("/opt/gar_xml.zip");

		}

		private bool CheckAvailableSpace(Uri uriPath)
		{
			string tempPath = Path.GetTempPath();
			var drive = new DriveInfo(tempPath);

			var fileSize = GetFileSize(uriPath);
			var reserve = 1073741824; //1GB
			var available = drive.AvailableFreeSpace < reserve ? 0 : drive.AvailableFreeSpace - reserve;

			var result = fileSize < available;
			if(!result)
			{
				Console.WriteLine($"Недостаточно свободного места!");
			}
			Console.WriteLine($"Требуется {ConvertToMegabytes(fileSize)}MB, свободно {ConvertToMegabytes(available)}MB");
			return result;
		}

		private long GetFileSize(Uri uriPath)
		{
			var webRequest = WebRequest.Create(uriPath);
			webRequest.Method = "HEAD";

			using(var webResponse = webRequest.GetResponse())
			{
				var fileSize = webResponse.Headers.Get("Content-Length");
				return Convert.ToInt64(fileSize);
			}
		}

		private int ConvertToMegabytes(long bytes)
		{
			return (int)(bytes / 1024 / 1024);
		}

		private string GetTempFilePath()
		{
			return Path.Combine(GetTempDirectoryPath(), "temp_fias_gar.zip");
		}

		private string GetTempDirectoryPath()
		{
			return Path.Combine(Path.GetTempPath(), "FIAS_GAR");
		}

		private void CreateTempDirectory()
		{
			ClearTemp();
			do
			{
				Directory.CreateDirectory(GetTempDirectoryPath());
			} while(!Directory.Exists(GetTempDirectoryPath()));
		}

		private void ClearTemp()
		{
			var tempDirectory = GetTempDirectoryPath();
			if(Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, true);
			}
		}

		private bool DownloadGarArchive(string url, string fileName)
		{
			using(WebClient webClient = new WebClient())
			{
				int lastPercent = -1;
				int? totalMb = null;
				webClient.DownloadProgressChanged += (s, e) => {
					if(lastPercent == e.ProgressPercentage)
					{
						return;
					}
					if(totalMb == null)
					{
						totalMb = ConvertToMegabytes(e.TotalBytesToReceive);
					}
					lastPercent = e.ProgressPercentage;
					Console.Write($"\rЗагрузка архива ФИАС ({totalMb}MB). {lastPercent}%");
				};
				var task = webClient.DownloadFileTaskAsync(url, fileName);
				task.Wait();
				Console.WriteLine();
				return task.IsCompletedSuccessfully;
			}
		}

		private string GetLastGarUrl(int lastDays)
		{
			if(lastDays <= 0)
			{
				throw new ArgumentException($"{lastDays} должен быть больше 0");
			}

			var today = DateTime.Today;
			string urlPrefix = @"https://fias-file.nalog.ru/downloads/";
			string urlsuffix = @"/gar_xml.zip";
			string fileUrl;
			for(int i = 0; i < lastDays; i++)
			{
				var date = DateTime.Today.AddDays(-i);
				fileUrl = $"{urlPrefix}{date.ToString("yyyy.MM.dd")}{urlsuffix}";
				if(CheckFileExists(fileUrl))
				{
					return fileUrl;
				}
			}
			throw new InvalidOperationException("Не удалось найти файл для загрузки за последние {lastDays} дней.");
		}

		private bool CheckFileExists(string url)
		{
			HttpWebResponse response = null;

			var request = WebRequest.CreateHttp(url);
			request.Method = "HEAD";

			try
			{
				response = (HttpWebResponse)request.GetResponse();
				return true;
			}
			catch(WebException ex)
			{
				return false;
			}
			finally
			{
				response?.Close();
			}
		}

		public void LoadFromFile(string garFile)
		{
			using(var fileStream = new FileStream(garFile, FileMode.Open, FileAccess.Read))
			using(var zipFile = new ZipFile(fileStream))
			{
				var fiasReaderFactory = new FiasReaderFactory(zipFile);
				var regionModel = new RegionModel(_sessionFactory);
				var levelModel = new LevelModel(fiasReaderFactory, _sessionFactory);
				var addressTypeModel = new AddressTypeModel(levelModel, fiasReaderFactory, _sessionFactory);
				var houseTypeModel = new HouseTypeModel(fiasReaderFactory, _sessionFactory);
				var apartmentTypeModel = new ApartmentTypeModel(fiasReaderFactory, _sessionFactory);
				var addressModel = new AddressModel(addressTypeModel, levelModel, fiasReaderFactory, _sessionFactory);
				var steadModel = new SteadModel(fiasReaderFactory, _sessionFactory);
				var houseModel = new HouseModel(houseTypeModel, fiasReaderFactory, _sessionFactory);
				var apartmentModel = new ApartmentModel(apartmentTypeModel, fiasReaderFactory, _sessionFactory);
				var reestrObjectModel = new ReestrObjectModel(levelModel, fiasReaderFactory, _sessionFactory);
				var hierarchyModel = new HierarchyModel(fiasReaderFactory, _sessionFactory);

				regionModel.CreateRegions();
				levelModel.LoadLevels();
				addressTypeModel.LoadAndUpdateAddressObjectTypes();
				houseTypeModel.LoadAndUpdateHouseTypes();			
				apartmentTypeModel.LoadAndUpdateApartmentTypes();

				var regions = regionModel.GetRegions();
				foreach(var region in regions)
				{
					addressModel.LoadAndUpdateAddressObjects(region.Code);
					steadModel.LoadAndUpdateSteads(region.Code);
					houseModel.LoadAndUpdateHouses(region.Code);
					apartmentModel.LoadAndUpdateApartments(region.Code);
					reestrObjectModel.LoadAndUpdateReestrObjects(region.Code);
					hierarchyModel.LoadAndUpdateHierarchy(region.Code);
				}
			}
		}
	}
}
