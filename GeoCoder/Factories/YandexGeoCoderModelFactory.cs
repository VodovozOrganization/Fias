using System.Net.Http;
using GeoCoder.Models;

namespace GeoCoder.Factories
{
	public class YandexGeoCoderModelFactory : IYandexGeoCoderModelFactory
	{
		public YandexGeoCoderModel GetNewYandexGeoCoderModel(HttpClient client, string yandexBaseUrl, string yandexApiKey) =>
			new YandexGeoCoderModel(client, yandexBaseUrl, yandexApiKey);
	}
}
