using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Fias.Source.Entities
{
	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_ADM_HIERARCHY")]
    [XmlType("ITEM", Namespace="", AnonymousType=true)]
	public partial class FiasAdmHierarchy
	{
        /// <summary>
        /// <para>Уникальный идентификатор записи. Ключевое поле</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public long Id { get; set; }
        
        /// <summary>
        /// <para>Глобальный уникальный идентификатор объекта</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("OBJECTID")]
        public long ObjectId { get; set; }
        
        /// <summary>
        /// <para>Идентификатор родительского объекта</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [XmlAttribute("PARENTOBJID")]
        public long ParentObjectId { get; set; }
        
        /// <summary>
        /// <para>ID изменившей транзакции</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("CHANGEID")]
        public long ChangeId { get; set; }
        
        /// <summary>
        /// <para>Код региона</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// <para xml:lang="en">Pattern: [0-9]{1,4}.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(4)]
        [RegularExpression("[0-9]{1,4}")]
        [XmlAttribute("REGIONCODE")]
        public string RegionCode { get; set; }
        
        /// <summary>
        /// <para>Код района</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// <para xml:lang="en">Pattern: [0-9]{1,4}.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(4)]
        [RegularExpression("[0-9]{1,4}")]
        [XmlAttribute("AREACODE")]
        public string AreaCode { get; set; }
        
        /// <summary>
        /// <para>Код города</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// <para xml:lang="en">Pattern: [0-9]{1,4}.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(4)]
        [RegularExpression("[0-9]{1,4}")]
        [XmlAttribute("CITYCODE")]
        public string CityCode { get; set; }
        
        /// <summary>
        /// <para>Код населенного пункта</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// <para xml:lang="en">Pattern: [0-9]{1,4}.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(4)]
        [RegularExpression("[0-9]{1,4}")]
        [XmlAttribute("PLACECODE")]
        public string PlaceCode { get; set; }
        
        /// <summary>
        /// <para>Код ЭПС</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// <para xml:lang="en">Pattern: [0-9]{1,4}.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(4)]
        [RegularExpression("[0-9]{1,4}")]
        [XmlAttribute("PLANCODE")]
        public string PlanCode { get; set; }
        
        /// <summary>
        /// <para>Код улицы</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 4.</para>
        /// <para xml:lang="en">Pattern: [0-9]{1,4}.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(4)]
        [RegularExpression("[0-9]{1,4}")]
        [XmlAttribute("STREETCODE")]
        public string StreetCode { get; set; }
        
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
        public DateTime UPDATEDATE { get; set; }
        
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
