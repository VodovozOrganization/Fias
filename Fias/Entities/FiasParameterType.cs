using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Fias.Source.Entities
{
	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_PARAM_TYPES")]
    [XmlType("PARAMTYPE", Namespace="", AnonymousType=true)]
	public partial class FiasParameterType
    {
        
        /// <summary>
        /// <para>Идентификатор типа параметра (ключ)</para>
        /// <para xml:lang="en">Total number of digits: 2.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public sbyte Id { get; set; }
        
        /// <summary>
        /// <para>Наименование</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 50.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(50)]
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
        [Required()]
        [XmlAttribute("CODE")]
        public string Code { get; set; }
        
        /// <summary>
        /// <para>Описание</para>
        /// <para xml:lang="en">Maximum length: 120.</para>
        /// </summary>
        [MaxLength(120)]
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
