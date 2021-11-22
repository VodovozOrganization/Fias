using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Fias.Source.Entities
{
	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_REESTR_OBJECTS")]
    [XmlType("OBJECT", Namespace="", AnonymousType=true)]
	public partial class FiasReestrObject
    {
        /// <summary>
        /// <para>Уникальный идентификатор объекта</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("OBJECTID")]
        public long Id { get; set; }

		/// <summary>
		/// <para>Уровень объекта</para>
		/// <para xml:lang="en">Total number of digits: 10.</para>
		/// </summary>
		[Required()]
		[XmlAttribute("LEVELID")]
		public int Level { get; set; }

		/// <summary>
		/// <para>GUID объекта</para>
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
		/// <para>Дата создания</para>
		/// </summary>
		[Required()]
        [XmlAttribute("CREATEDATE", DataType="date")]
        public DateTime CreateDate { get; set; }

		/// <summary>
		/// <para>Дата обновления</para>
		/// </summary>
		[Required()]
		[XmlAttribute("UPDATEDATE", DataType = "date")]
		public DateTime UpdateDate { get; set; }
        
        /// <summary>
        /// <para>Признак действующего объекта (1 - действующий, 0 - не действующий)</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ISACTIVE")]
        public string IsActiveValue { get; set; }

		/// <summary>
		/// <para>Признак действующего объекта (1 - действующий, 0 - не действующий)</para>
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
