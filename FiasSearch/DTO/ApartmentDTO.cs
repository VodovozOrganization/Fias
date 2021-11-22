using System;

namespace Fias.Search.DTO
{
	public class ApartmentDTO
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
		/// Номер помещения
		/// </summary>
		public string Number { get; set; }

		/// <summary>
		/// Название типа помещения
		/// </summary>
		public string TypeName { get; set; }

		/// <summary>
		/// Сокращенное название типа помещения
		/// </summary>
		public string TypeShortName { get; set; }

		/// <summary>
		/// Описание название помещения
		/// </summary>
		public string TypeDescription { get; set; }
	}
}
