using Fias.Search;
using Fias.Search.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GeoCoder.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FiasApi.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class FiasController : ControllerBase
	{
		private readonly IHttpClientFactory _clientFactory;
		private readonly IConfiguration _configuration;
		private readonly ILogger<FiasController> _logger;
		private readonly ApartmentRepository _apartmentRepository;
		private readonly CityRepository _cityRepository;
		private readonly HouseRepository _houseRepository;
		private readonly StreetRepository _streetRepository;

		public FiasController(IHttpClientFactory clientFactory, IConfiguration configuration, ILogger<FiasController> logger, ISessionFactory sessionFactory)
		{
			_clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_apartmentRepository = new ApartmentRepository(sessionFactory);
			_cityRepository = new CityRepository(sessionFactory);
			_houseRepository = new HouseRepository(sessionFactory);
			_streetRepository = new StreetRepository(sessionFactory);
		}

		[HttpGet("/api/GetCitiesByCriteria")]
		public IEnumerable<CityDTO> GetCitiesByCriteria(string searchString, int limit, bool isActive = true)
		{
			return _cityRepository.GetCities(searchString, limit, isActive);
		}

		[HttpGet("/api/GetCities")]
		public IEnumerable<CityDTO> GetCities(int limit, bool isActive = true)
		{
			return _cityRepository.GetCities(limit, isActive);
		}

		[HttpGet("/api/GetStreetsByCriteria")]
		public IEnumerable<StreetDTO> GetStreetsByCriteria(Guid cityGuid, string searchString, int limit, bool isActive = true)
		{
			return _streetRepository.GetStreets(searchString, cityGuid, limit, isActive);
		}

		[HttpGet("/api/GetHousesFromStreet")]
		public IEnumerable<HouseDTO> GetHousesFromStreet(Guid streetGuid, int? limit = null, bool isActive = true)
		{
			return _houseRepository.GetHousesFromStreet(streetGuid, limit, isActive).OrderBy(x=>x.HouseObjectType);
		}

		[HttpGet("/api/GetHousesFromStreetByCriteria")]
		public IEnumerable<HouseDTO> GetHousesFromStreetByCriteria(Guid streetGuid, string searchString, int? limit = null, bool isActive = true)
		{
			return _houseRepository.GetHousesFromStreet(searchString, streetGuid, limit, isActive);
		}

		[HttpGet("/api/GetHousesFromCityByCriteria")]
		public IEnumerable<HouseDTO> GetHousesFromCityByCriteria(Guid cityGuid, string searchString, int? limit = null, bool isActive = true)
		{
			return _houseRepository.GetHousesFromCity(searchString, cityGuid, limit, isActive);
		}

		[HttpGet("/api/GetCoordinatesByGeoCoder")]
		public async Task<PointDTO> GetCoordinatesByGeoCoderAsync(string address)
		{
			if(address == null)
			{
				return null;
			}

			var client = _clientFactory.CreateClient();
			var yandexBaseUrl = _configuration.GetValue<string>("Security:YandexBaseUrl");
			var yandexApiKey = _configuration.GetValue<string>("Security:YandexApiKey");

			var geoCoderList = new List<IGeoCoderModel>
			{
				new YandexGeoCoderModel(client, yandexBaseUrl, yandexApiKey)
			};

			foreach(var geoCoder in geoCoderList)
			{
				var geoDto = await geoCoder.GetCoordinatesAsync(address);

				if(geoDto != null)
				{
					return new PointDTO
					{
						Latitude = geoDto.Latitude,
						Longitude = geoDto.Longitude
					};
				}
			}

			return null;
		}
	}
}
