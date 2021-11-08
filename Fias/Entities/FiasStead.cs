namespace Fias.Entities
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Xml.Serialization;


	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_STEADS")]
    [XmlType("STEAD", Namespace="", AnonymousType=true)]
	public partial class FiasStead
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
        /// <para>Номер земельного участка</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 250.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(250)]
        [Required()]
        [XmlAttribute("NUMBER")]
        public string Number { get; set; }
        
        /// <summary>
        /// <para>Статус действия над записью – причина появления записи</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 2.</para>
        /// <para xml:lang="en">Pattern: [0-9]{1,2}.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(2)]
        [RegularExpression("[0-9]{1,2}")]
        [Required()]
        [XmlAttribute("OPERTYPEID")]
        public string OperationTypeId { get; set; }
        
        /// <summary>
        /// <para>Идентификатор записи связывания с предыдущей исторической записью</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [XmlAttribute("PREVID")]
        public decimal PreviousId { get; set; }
        
        /// <summary>
        /// <para>Идентификатор записи связывания с последующей исторической записью</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [XmlAttribute("NEXTID")]
        public decimal NextId { get; set; }
        
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
		/// </summary>
		[Required()]
		[XmlAttribute("ISACTIVE")]
		public string IsActiveValue { get; set; }


		/// <summary>
		/// <para>Статус актуальности адресного объекта ФИАС</para>
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
