/****************************************************
    文件：IMeshModifier.cs
    作者：JiahaoWu
    邮箱: jiahaodev@163.ccom
    日期：2020/02/25 15:23       
    功能：【接口】修改Mesh数据的统一接口
*****************************************************/
using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use IMeshModifier instead", true)]
    public interface IVertexModifier
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts)  instead", true)]
        void ModifyVertices(List<UIVertex> verts);
    }

    public interface IMeshModifier
    {
        [Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead", false)]
        void ModifyMesh(Mesh mesh);
        void ModifyMesh(VertexHelper verts); //本文件中，唯一仅存的推荐接口
    }
}
