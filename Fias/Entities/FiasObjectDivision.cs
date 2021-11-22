using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Fias.Source.Entities
{
	[Serializable()]
	[FiasFile(FiasFileTypeName = "AS_ADDR_OBJ_DIVISION")]
    [XmlType("ITEM", Namespace="", AnonymousType=true)]
	public partial class FiasObjectDivision
    {
        /// <summary>
        /// <para>Уникальный идентификатор записи. Ключевое поле</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("ID")]
        public long Id { get; set; }
        
        /// <summary>
        /// <para>Родительский ID</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("PARENTID")]
        public long ParentId { get; set; }
        
        /// <summary>
        /// <para>Дочерний ID</para>
        /// <para xml:lang="en">Total number of digits: 19.</para>
        /// </summary>
        [Required()]
        [XmlAttribute("CHILDID")]
        public long ChildId { get; set; }
        
        /// <summary>
        /// <para>ID изменившей транзакции</para>
        /// </summary>
        [Required()]
        [XmlAttribute("CHANGEID")]
        public long ChangeId { get; set; }
    }
}
