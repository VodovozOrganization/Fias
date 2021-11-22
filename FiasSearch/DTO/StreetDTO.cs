using System;

namespace Fias.Search.DTO
{
	public class StreetDTO
	{
		/// <summary>
		/// Идентификатор во внутренней базе
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Глобальный идентификатор ФИАС
		/// </summary>
		public Guid FiasGuid { get; set; }

		/// <summary>
		/// Название улицы
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Название типа улицы
		/// </summary>
		public string TypeName { get; set; }

		/// <summary>
		/// Сокращенное название типа улицы
		/// </summary>
		public string TypeShortName { get; set; }

		/// <summary>
		/// Описание типа улицы
		/// </summary>
		public string TypeDescription { get; set; }
	}
}
