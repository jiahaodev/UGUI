/****************************************************
    文件：RaycasterManager.cs
    作者：JiahaoWu
    邮箱: jiahaodev@163.ccom
    日期：2020/02/25 11:15       
    功能：各种射线照射器的管理者
*****************************************************/
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    internal static class RaycasterManager
    {
        //管理核心：射线投射器 列表
        private static readonly List<BaseRaycaster> s_Raycasters = new List<BaseRaycaster>();

        public static void AddRaycaster(BaseRaycaster baseRaycaster)
        {
            if (s_Raycasters.Contains(baseRaycaster))
                return;

            s_Raycasters.Add(baseRaycaster);
        }

        public static List<BaseRaycaster> GetRaycasters()
        {
            return s_Raycasters;
        }

        public static void RemoveRaycasters(BaseRaycaster baseRaycaster)
        {
            if (!s_Raycasters.Contains(baseRaycaster))
                return;
            s_Raycasters.Remove(baseRaycaster);
        }
    }
}
