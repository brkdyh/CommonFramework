/*
	Auto Generated By SampleECS,Don't Modify It Manually!
*/
using System.Collections.Generic;
namespace SampleECS
{
	public static partial class Game_Component_Type
	{
		/******** Begin Static Components Code ********/
		public const int Static_MsgComp = 0;
		/******** End Static Components Code ********/

		/* Component <IDComp> ID */
		public const int IDComp = 0;
		/* Component <PositionComp> ID */
		public const int PositionComp = 1;
		/* Component <TransformComp> ID */
		public const int TransformComp = 2;
		/* Component <CreateComp> ID */
		public const int CreateComp = 3;
	}

	public static partial class Game_Component_Type
	{
		public const int STATIC_TYPE_COUNT = 1;

		public static int COMPONENT_TYPE_COUNT { get; private set; } = 0;
		internal static void SetTypeCount(int count) { COMPONENT_TYPE_COUNT = count; }
		static Dictionary<string, int> COM_TYPE_ID_MAP = new Dictionary<string, int>();

		static bool inited = false;
		public static void Init()
		{
			if(inited) return;
			COM_TYPE_ID_MAP.Add("IDComp",IDComp);
			COM_TYPE_ID_MAP.Add("PositionComp",PositionComp);
			COM_TYPE_ID_MAP.Add("TransformComp",TransformComp);
			COM_TYPE_ID_MAP.Add("CreateComp",CreateComp);
			SetTypeCount(COM_TYPE_ID_MAP.Count);
			inited = true;
		}
	}
}