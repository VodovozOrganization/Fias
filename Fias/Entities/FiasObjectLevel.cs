namespace Fias.Entities
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Xml.Serialization;


	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_OBJECT_LEVELS")]
    [XmlType("OBJECTLEVEL", Namespace="", AnonymousType=true)]
    public partial class FiasObjectLevel
    {
        
        /// <summary>
        /// <para>Уникальный идентификатор записи. Ключевое поле. Номер уровня объекта</para>
        /// <para xml:lang="en">Total number of digits: 2.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("LEVEL")]
        public sbyte Level { get; set; }
        
        /// <summary>
        /// <para>Наименование</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 250.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(250)]
        [Required()]
        [XmlAttribute("NAME")]
        public string Name { get; set; }
        
        /// <summary>
        /// <para>Краткое наименование</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 50.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(50)]
        [XmlAttribute("SHORTNAME")]
        public string ShortName { get; set; }
        
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
        /// <para>Признак действующего адресного объекта</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ISACTIVE")]
        public bool IsActive { get; set; }
    }
}
