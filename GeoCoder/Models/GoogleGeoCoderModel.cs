using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using GeoCoder.Dto;

namespace GeoCoder.Models
{
	public class GoogleGeoCoderModel : IGeoCoderModel
	{
		public async Task<GeoCoordinateDto> GetCoordinatesAsync(string address)
		{
			var requestUri = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false", Uri.EscapeDataString(address));

			using(var client = new HttpClient())
			{
				var request = await client.GetAsync(requestUri);
				var content = await request.Content.ReadAsStringAsync();
				var xmlDocument = XDocument.Parse(content);

				XElement result = xmlDocument.Element("GeocodeResponse").Element("result");
				XElement locationElement = result.Element("geometry").Element("location");
				XElement lat = locationElement.Element("lat");
				XElement lng = locationElement.Element("lng");
				return new GeoCoordinateDto
				{
					Latitude = lat.Value,
					Longitude = lng.Value
				};
				return null;
			}
		}
		public Task<string> GetAddressAsync(float latitude, float longitude)
		{
			throw new NotSupportedException("Currently getting address via google is not supported by the library.");
		}

	}
}
