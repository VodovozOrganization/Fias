using System.Net.Http;
using GeoCoder.Models;

namespace GeoCoder.Factories
{
	public interface IYandexGeoCoderModelFactory
	{
		YandexGeoCoderModel GetNewYandexGeoCoderModel(HttpClient client, string yandexBaseUrl, string yandexApiKey);
	}
}
