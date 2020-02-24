/****************************************************
	文件：ListPool.cs
	作者：JiahaoWu
	邮箱: jiahaodev@163.ccom
	日期：2020/02/25 0:55   	
	功能：对ObjectPool的进一步封装，用于存放List<T>类型数据作为对象池内容
*****************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    internal static class ListPool<T>
    {
        // Object pool to avoid allocations.
        private static readonly ObjectPool<List<T>> s_ListPool = new ObjectPool<List<T>>(null, Clear);
        static void Clear(List<T> l) { l.Clear(); }

        public static List<T> Get()
        {
            return s_ListPool.Get();
        }

        public static void Release(List<T> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }
}
