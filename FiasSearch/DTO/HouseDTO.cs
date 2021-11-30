using System;

namespace Fias.Search.DTO
{
	public class HouseDTO
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
		/// Тип объекта
		/// </summary>
		public HouseObjectType HouseObjectType { get; set; }

		/// <summary>
		/// Номер объекта.
		/// Применим для домов и земельных участков
		/// </summary>
		public string ObjectNumber { get; set; }

		/// <summary>
		/// Название типа дома.
		/// Применим только для домов
		/// </summary>
		public string HouseTypeName { get; set; }

		/// <summary>
		/// Сокращенное название типа дома.
		/// Применим только для домов
		/// </summary>
		public string HouseTypeShortName { get; set; }

		/// <summary>
		/// Описание название типа дома.
		/// Применим только для домов
		/// </summary>
		public string HouseTypeDescription { get; set; }

		/// <summary>
		/// Первый добавочный номер дома.
		/// Применим только для домов
		/// </summary>
		public string AddNumber1 { get; set; }

		/// <summary>
		/// Название типа первого добавочного номера дома.
		/// Применим только для домов
		/// </summary>
		public string AddType1Name { get; set; }

		/// <summary>
		/// Сокращенное название типа первого добавочного номера дома.
		/// Применим только для домов
		/// </summary>
		public string AddType1ShortName { get; set; }

		/// <summary>
		/// Описание типа первого добавочного номера дома.
		/// Применим только для домов
		/// </summary>
		public string AddType1Description { get; set; }

		/// <summary>
		/// Второй добавочный номер дома.
		/// Применим только для домов
		/// </summary>
		public string AddNumber2 { get; set; }

		/// <summary>
		/// Название типа второго добавочного номера дома.
		/// Применим только для домов
		/// </summary>
		public string AddType2Name { get; set; }

		/// <summary>
		/// Сокращенное название типа второго добавочного номера дома.
		/// Применим только для домов
		/// </summary>
		public string AddType2ShortName { get; set; }

		/// <summary>
		/// Описание типа второго добавочного номера дома.
		/// Применим только для домов
		/// </summary>
		public string AddType2Description { get; set; }

		/// <summary>
		/// Широта
		/// </summary>
		public string Latitude { get; set; }

		/// <summary>
		/// Долгота
		/// </summary>
		public string Longitude { get; set; }

		public string ComplexNumber
		{
			get
			{
				var houseName = string.Empty;

				if(!string.IsNullOrWhiteSpace(ObjectNumber))
				{
					houseName += $"{ObjectNumber}";
				}

				if(!string.IsNullOrWhiteSpace(AddType1ShortName))
				{
					houseName += $", {AddType1ShortName}";
				}

				if(!string.IsNullOrWhiteSpace(AddNumber1))
				{
					houseName += $" {AddNumber1}";
				}

				if(!string.IsNullOrWhiteSpace(AddType2ShortName))
				{
					houseName += $", {AddType2ShortName}";
				}

				if(!string.IsNullOrWhiteSpace(AddNumber2))
				{
					houseName += $" {AddNumber2}";
				}

				return houseName;
			}
		}
	}
}
