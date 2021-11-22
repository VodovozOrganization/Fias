namespace Fias.Search
{
	public class HouseObjectTypeStringType : NHibernate.Type.EnumStringType
	{
		public HouseObjectTypeStringType() : base(typeof(HouseObjectType))
		{
		}
	}
}
