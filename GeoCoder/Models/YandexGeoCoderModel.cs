using GeoCoder.Dto;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;

namespace GeoCoder.Models
{
	public class YandexGeoCoderModel : IGeoCoderModel
	{
		private readonly string _apiKey;
		private readonly HttpClient _client;
		private readonly string _baseUrl;

		public YandexGeoCoderModel(HttpClient client, string baseUrl, string apiKey)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl)); ;
			_apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
		}
		public async Task<GeoCoordinateDto> GetCoordinatesAsync(string address)
		{
			string requestParams = $"{ _baseUrl }?apikey={ _apiKey }&geocode={ Uri.EscapeDataString(address) }";

			XmlDocument doc = new XmlDocument();

			_client.BaseAddress = new Uri(_baseUrl);
			_client.DefaultRequestHeaders.Accept.Clear();
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

			var response = await _client.GetAsync(requestParams);

			if(response.IsSuccessStatusCode)
			{
				var product = await response.Content.ReadAsStringAsync();
				doc.LoadXml(product);
			}

			bool isExactCoordinates =
				doc["ymaps"]?["GeoObjectCollection"]?["featureMember"]?["GeoObject"]?["metaDataProperty"]?["GeocoderMetaData"]?
					["precision"]?.InnerText == "exact";

			if(!isExactCoordinates)
			{
				return null;
			}

			XmlNode posNode = doc["ymaps"]?["GeoObjectCollection"]?["featureMember"]?["GeoObject"]?["Point"]?["pos"];

			if(posNode == null)
			{
				return null;
			}

			var pos = posNode.InnerText.Split(' ');

			return new GeoCoordinateDto
			{
				Latitude = pos[1],
				Longitude = pos[0]
			};
		}

		public async Task<string> GetAddressAsync(float latitude, float longitude)
		{
			var point = $"{longitude},{latitude}";

			string requestParams = $"{_baseUrl}?apikey={_apiKey}&geocode={Uri.EscapeDataString(point)}";

			XmlDocument doc = new XmlDocument();

			_client.BaseAddress = new Uri(_baseUrl);
			_client.DefaultRequestHeaders.Accept.Clear();
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

			var response = await _client.GetAsync(requestParams);

			if(response.IsSuccessStatusCode)
			{
				var product = await response.Content.ReadAsStringAsync();
				doc.LoadXml(product);
			}

			bool isExactCoordinates =
				doc["ymaps"]?["GeoObjectCollection"]?["featureMember"]?["GeoObject"]?["metaDataProperty"]?["GeocoderMetaData"]?
					["precision"]?.InnerText == "exact";

			if(!isExactCoordinates)
			{
				return null;
			}

			XmlNode posNode = doc["ymaps"]?["GeoObjectCollection"]?["featureMember"]?["GeoObject"]?["metaDataProperty"]?["GeocoderMetaData"]
				?["AddressDetails"]?["Country"]?["AdministrativeArea"]?["Locality"];

			if(posNode == null)
			{
				return null;
			}

			var city = posNode?["LocalityName"].InnerText;
			var street = posNode?["Thoroughfare"]?["ThoroughfareName"].InnerText;
			var house = posNode?["Thoroughfare"]?["Premise"]?["PremiseNumber"].InnerText;

			return $"{city}, {street}, {house}";
		}
	}
}
