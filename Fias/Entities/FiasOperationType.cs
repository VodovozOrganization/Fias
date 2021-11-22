using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Fias.Source.Entities
{
	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_OPERATION_TYPES")]
    [XmlType("OPERATIONTYPE", Namespace="", AnonymousType=true)]
	public partial class FiasOperationType
    {
        
        /// <summary>
        /// <para>Идентификатор статуса (ключ)</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public string Id { get; set; }
        
        /// <summary>
        /// <para>Наименование</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 100.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(100)]
        [Required()]
        [XmlAttribute("NAME")]
        public string Name { get; set; }
        
        /// <summary>
        /// <para>Краткое наименование</para>
        /// <para xml:lang="en">Maximum length: 100.</para>
        /// </summary>
        [MaxLength(100)]
        [XmlAttribute("SHORTNAME")]
        public string ShortName { get; set; }
        
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
