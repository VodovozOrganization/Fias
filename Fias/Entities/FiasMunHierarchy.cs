using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Fias.Source.Entities
{
	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_MUN_HIERARCHY")]
    [XmlType("ITEM", Namespace="", AnonymousType=true)]
	public partial class FiasMunHierarchy
    {
        /// <summary>
        /// <para>Уникальный идентификатор записи. Ключевое поле</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public long Id { get; set; }
        
        /// <summary>
        /// <para>Глобальный уникальный идентификатор адресного объекта</para>
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
        /// <para>Код ОКТМО</para>
        /// <para xml:lang="en">Maximum length: 11.</para>
        /// <para xml:lang="en">Pattern: [0-9]{8,11}.</para>
        /// </summary>
        [MaxLength(11)]
        [RegularExpression("[0-9]{8,11}")]
        [XmlAttribute("OKTMO")]
        public string OktmoCode { get; set; }
        
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
