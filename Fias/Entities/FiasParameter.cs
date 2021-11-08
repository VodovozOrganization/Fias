namespace Fias.Entities
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Xml.Serialization;


	[Serializable()]
    [XmlType("PARAM", Namespace="", AnonymousType=true)]
	public partial class FiasParameter
    {
        
        /// <summary>
        /// <para>Идентификатор записи</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public long ID { get; set; }
        
        /// <summary>
        /// <para>Глобальный уникальный идентификатор адресного объекта</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("OBJECTID")]
        public long ObjectId { get; set; }
        
        /// <summary>
        /// <para>ID изменившей транзакции</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [XmlAttribute("CHANGEID")]
        public long ChangeId { get; set; }
        
        /// <summary>
        /// <para>ID завершившей транзакции</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("CHANGEIDEND")]
        public long ChangeIdEnd { get; set; }
        
        /// <summary>
        /// <para>Тип параметра</para>
        /// <para xml:lang="en">Total number of digits: 4.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("TYPEID")]
        public short TypeId { get; set; }
        
        /// <summary>
        /// <para>Значение параметра</para>
        /// <para xml:lang="en">Minimum length: 1.</para>
        /// <para xml:lang="en">Maximum length: 8000.</para>
        /// </summary>
        [MinLength(1)]
        [MaxLength(8000)]
        [Required()]
        [XmlAttribute("VALUE")]
        public string Value { get; set; }
        
        /// <summary>
        /// <para>Дата внесения (обновления) записи</para>
        /// </summary>
        [Required()]
        [XmlAttribute("UPDATEDATE", DataType="date")]
        public DateTime UpdateDate { get; set; }
        
        /// <summary>
        /// <para>Дата начала действия записи</para>
        /// </summary>
        [Required()]
        [XmlAttribute("STARTDATE", DataType="date")]
        public DateTime StartDate { get; set; }
        
        /// <summary>
        /// <para>Дата окончания действия записи</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ENDDATE", DataType="date")]
        public DateTime EndDate { get; set; }
    }
}
