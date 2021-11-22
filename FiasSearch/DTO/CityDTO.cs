using System;

namespace Fias.Search.DTO
{
	public class CityDTO
	{
		/// <summary>
		/// Идентификатор во внутренней базе
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Код региона
		/// </summary>
		public int RegionCode { get; set; }

		/// <summary>
		/// Название региона
		/// </summary>
		public string RegionName { get; set; }

		/// <summary>
		/// Глобальный идентификатор ФИАС
		/// </summary>
		public Guid FiasGuid { get; set; }

		/// <summary>
		/// Название населенного пункта
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Название типа населенного пункта
		/// </summary>
		public string TypeName { get; set; }

		/// <summary>
		/// Сокращенное название типа населенного пункта
		/// </summary>
		public string TypeShortName { get; set; }
	}
}
