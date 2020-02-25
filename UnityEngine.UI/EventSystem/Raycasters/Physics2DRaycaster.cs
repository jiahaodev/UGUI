/****************************************************
    文件：Physics2DRaycaster.cs
    作者：JiahaoWu
    邮箱: jiahaodev@163.ccom
    日期：2020/02/25 11:15       
    功能：2D物理射线照射器
*****************************************************/
using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Simple event system using physics raycasts.
    /// </summary>
    [AddComponentMenu("Event/Physics 2D Raycaster")]
    [RequireComponent(typeof(Camera))]
    /// <summary>
    /// Raycaster for casting against 2D Physics components.
    /// </summary>
    public class Physics2DRaycaster : PhysicsRaycaster
    {
        RaycastHit2D[] m_Hits;

        protected Physics2DRaycaster()
        {}

        /// <summary>
        /// Raycast against 2D elements in the scene.
        /// </summary>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            Ray ray = new Ray();
            float distanceToClipPlane = 0;
            if (!ComputeRayAndDistance(eventData, ref ray, ref distanceToClipPlane))
                return;

            int hitCount = 0;

            if (maxRayIntersections == 0)
            {
                if (ReflectionMethodsCache.Singleton.getRayIntersectionAll == null)
                    return;
                //采用ReflectionMethodsCache.Singleton.getRayIntersectionAll()【3D模式采用raycast3DAll()】来获取所有射线照射到的对象，
                //用反射的方式把Physics.RaycastAll()方法缓存下来，让Unity的Physics模块与UI模块，保持低耦合，没有过分依赖。
                m_Hits = ReflectionMethodsCache.Singleton.getRayIntersectionAll(ray, distanceToClipPlane, finalEventMask);
                hitCount = m_Hits.Length;
            }
            else
            {
                if (ReflectionMethodsCache.Singleton.getRayIntersectionAllNonAlloc == null)
                    return;

                if (m_LastMaxRayIntersections != m_MaxRayIntersections)
                {
                    m_Hits = new RaycastHit2D[maxRayIntersections];
                    m_LastMaxRayIntersections = m_MaxRayIntersections;
                }

                hitCount = ReflectionMethodsCache.Singleton.getRayIntersectionAllNonAlloc(ray, m_Hits, distanceToClipPlane, finalEventMask);
            }

            //由于是2D,所以这里理论上距离是相同的，不需要排序

            if (hitCount != 0)
            {
                for (int b = 0, bmax = hitCount; b < bmax; ++b)
                {
                    var sr = m_Hits[b].collider.gameObject.GetComponent<SpriteRenderer>();

                    var result = new RaycastResult
                    {
                        gameObject = m_Hits[b].collider.gameObject,
                        module = this,
                        distance = Vector3.Distance(eventCamera.transform.position, m_Hits[b].point),
                        worldPosition = m_Hits[b].point,
                        worldNormal = m_Hits[b].normal,
                        screenPosition = eventData.position,
                        index = resultAppendList.Count,
                        sortingLayer =  sr != null ? sr.sortingLayerID : 0,
                        sortingOrder = sr != null ? sr.sortingOrder : 0
                    };
                    resultAppendList.Add(result);
                }
            }
        }
    }
}
