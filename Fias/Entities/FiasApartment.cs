using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Fias.Source.Entities
{
	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_APARTMENTS")]
    [XmlType("APARTMENT", Namespace="", AnonymousType=true)]
	public partial class FiasApartment
    {
        /// <summary>
        /// <para>Уникальный идентификатор записи. Ключевое поле</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public long Id { get; set; }
        
        /// <summary>
        /// <para>Глобальный уникальный идентификатор объекта типа INTEGER</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("OBJECTID")]
        public long ObjectId { get; set; }
        
        /// <summary>
        /// <para>Глобальный уникальный идентификатор адресного объекта типа UUID</para>
        /// <para xml:lang="en">Minimum length: 36.</para>
        /// <para xml:lang="en">Maximum length: 36.</para>
        /// </summary>
        [MinLength(36)]
        [MaxLength(36)]
        [Required()]
        [XmlAttribute("OBJECTGUID")]
        public string ObjectGuid { get; set; }
        
        /// <summary>
        /// <para>ID изменившей транзакции</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("CHANGEID")]
        public long ChangeId { get; set; }
        
        /// <summary>
        /// <para>Номер комнаты</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 50.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(50)]
        [Required()]
        [XmlAttribute("NUMBER")]
        public string Number { get; set; }
        
        /// <summary>
        /// <para>Тип комнаты</para>
        /// <para xml:lang="en">Total number of digits: 2.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("APARTTYPE")]
        public sbyte ApartmentType { get; set; }
        
        /// <summary>
        /// <para>Статус действия над записью – причина появления записи</para>
        /// <para xml:lang="en">Total number of digits: 2.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("OPERTYPEID")]
        public long OperationId { get; set; }
        
        /// <summary>
        /// <para>Идентификатор записи связывания с предыдущей исторической записью</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [XmlAttribute("PREVID")]
        public long PreviousId { get; set; }
        
        /// <summary>
        /// <para>Идентификатор записи связывания с последующей исторической записью</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [XmlAttribute("NEXTID")]
        public long NextId { get; set; }
        
        /// <summary>
        /// <para>Дата внесения (обновления) записи</para>
        /// </summary>
        [Required()]
        [XmlAttribute("UPDATEDATE", DataType="date")]
        public DateTime UpdateDate { get; set; }
        
        /// <summary>
        /// <para>Начало действия записи</para>
        /// </summary>
        [Required()]
        [XmlAttribute("STARTDATE", DataType="date")]
        public DateTime StartDate { get; set; }
        
        /// <summary>
        /// <para>Окончание действия записи</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ENDDATE", DataType="date")]
        public DateTime EndDate { get; set; }
        
        /// <summary>
        /// <para>Статус актуальности адресного объекта ФИАС</para>
        /// <para xml:lang="en">Total number of digits: 1.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ISACTUAL")]
        public string IsActualValue { get; set; }

		/// <summary>
		/// <para>Статус актуальности адресного объекта ФИАС</para>
		/// </summary>
		[XmlIgnore()]
		public bool IsActual
		{
			get
			{
				return IsActualValue == "1";
			}
			set
			{
				IsActualValue = value ? "1" : "0";
			}
		}

		/// <summary>
		/// <para>Признак действующего адресного объекта</para>
		/// <para xml:lang="en">Total number of digits: 1.</para>
		/// </summary>
		[Required()]
        [XmlAttribute("ISACTIVE")]
        public string IsActiveValue { get; set; }


		/// <summary>
		/// <para>Признак действующего адресного объекта</para>
		/// </summary>
		[XmlIgnore()]
		public bool IsActive
		{
			get
			{
				return IsActiveValue == "1";
			}
			set
			{
				IsActiveValue = value ? "1" : "0";
			}
		}
	}
}
