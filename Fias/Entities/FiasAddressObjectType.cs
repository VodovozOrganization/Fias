namespace Fias.Entities
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Xml.Serialization;


	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_ADDR_OBJ_TYPES")]
    [XmlType("ADDRESSOBJECTTYPE", Namespace="", AnonymousType=true)]
	public partial class FiasAddressObjectType
    {
        
        /// <summary>
        /// <para>Идентификатор записи</para>
        /// <para xml:lang="en">Total number of digits: 10.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public long Id { get; set; }
        
        /// <summary>
        /// <para>Уровень адресного объекта</para>
        /// </summary>
        [Required()]
        [XmlAttribute("LEVEL")]
        public string Level { get; set; }
        
        /// <summary>
        /// <para>Краткое наименование типа объекта</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 50.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(50)]
        [Required()]
        [XmlAttribute("SHORTNAME")]
        public string ShortName { get; set; }
        
        /// <summary>
        /// <para>Полное наименование типа объекта</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 250.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(250)]
        [Required()]
        [XmlAttribute("NAME")]
        public string Name { get; set; }
        
        /// <summary>
        /// <para>Описание</para>
        /// <para xml:lang="en">Maximum length: 250.</para>
        /// </summary>
        [MaxLength(250)]
        [XmlAttribute("DESC")]
        public string Description { get; set; }
        
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
        /// <para>Статус активности</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ISACTIVE")]
        public bool IsActive { get; set; }
    }
}
