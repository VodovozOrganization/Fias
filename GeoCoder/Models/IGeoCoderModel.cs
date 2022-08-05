using System.Threading.Tasks;
using GeoCoder.Dto;

namespace GeoCoder.Models
{
	public interface IGeoCoderModel
	{
		public Task<GeoCoordinateDto> GetCoordinatesAsync( string address);
		public Task<string> GetAddressAsync(float latitude, float longitude);
	}
}
