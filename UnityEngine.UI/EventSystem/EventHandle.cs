/****************************************************
	文件：EventHandle.cs
	作者：JiahaoWu
	邮箱: jiahaodev@163.ccom
	日期：2020/02/24 23:49   	
	功能：【枚举】事件的状态（使用/未使用--》是否消耗掉）
*****************************************************/
using System;

namespace UnityEngine.EventSystems
{
    [Flags]
    /// <summary>
    /// Enum that tracks event State.
    /// </summary>
    public enum EventHandle
    {
        Unused = 0,
        Used = 1
    }
}
