/****************************************************
    文件：CanvasUpdateRegistry.cs
    作者：JiahaoWu
    邮箱: jiahaodev@163.ccom
    日期：2020/02/25 12:22       
    功能：画布更新登记（注册）
    todo: 实战使用，可以通过外部可以通过检测这两个序列，知道是哪些元素导致了rebuild
          https://www.cnblogs.com/chenggg/p/11184784.html  中NewBehaviourScript.cs
*****************************************************/
using System;
using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// Values of 'update' called on a Canvas update.
    /// </summary>
    /// 【枚举】除了最后一个枚举项，其他五个项分别代表了“布局的三个阶段”和“渲染的两个阶段”。
    public enum CanvasUpdate
    {
        /// <summary>
        /// Called before layout.
        /// </summary>
        Prelayout = 0,
        /// <summary>
        /// Called for layout.
        /// </summary>
        Layout = 1,
        /// <summary>
        /// Called after layout.
        /// </summary>
        PostLayout = 2,
        /// <summary>
        /// Called before rendering.
        /// </summary>
        PreRender = 3,
        /// <summary>
        /// Called late, before render.（PreRender之后，render执行之前）
        /// </summary>
        LatePreRender = 4,
        /// <summary>
        /// Max enum value. Always last.
        /// </summary>
        MaxUpdateValue = 5
    }

    /// <summary>
    /// This is an element that can live on a Canvas.
    /// </summary>
    /// Canvas上“存活”子对象需要实现的接口
    public interface ICanvasElement
    {
        /// <summary>
        /// Rebuild the element for the given stage.
        /// </summary>
        /// <param name="executing">The current CanvasUpdate stage being rebuild.</param>
        /// executing 表示画布更新的阶段（5个阶段），为枚举值。见CanvasUpdate
        void Rebuild(CanvasUpdate executing);

        /// <summary>
        /// Get the transform associated with the ICanvasElement.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Callback sent when this ICanvasElement has completed layout.
        /// </summary>
        /// 布局更新完成
        void LayoutComplete();

        /// <summary>
        /// Callback sent when this ICanvasElement has completed Graphic rebuild.
        /// </summary>
        /// 图形更新完成
        void GraphicUpdateComplete();

        /// <summary>
        /// Used if the native representation has been destroyed.
        /// </summary>
        /// <returns>Return true if the element is considered destroyed.</returns>
        bool IsDestroyed();
    }

    /// <summary>
    /// A place where CanvasElements can register themselves for rebuilding.
    /// </summary>
    /// 【单例】它是UGUI与Canvas之间的中介，实现了ICanvasElement接口的组件都可以注册到它，
    /// 它监听了Canvas即将渲染的事件（其实是委托），并调用已注册组件的Rebuild等方法。
    /// https://edu.uwa4d.com/lesson-detail/79/99/0?isPreview=0
    public class CanvasUpdateRegistry
    {
        private static CanvasUpdateRegistry s_Instance;

        private bool m_PerformingLayoutUpdate;
        private bool m_PerformingGraphicUpdate;
        //布局重建序列索引集（核心1）
        private readonly IndexedSet<ICanvasElement> m_LayoutRebuildQueue = new IndexedSet<ICanvasElement>();
        //图形重建序列索引集（核心2）
        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new IndexedSet<ICanvasElement>();

        //注意：IndexedSet是UGUI内部自定义的集合类，可以自行查看源定义


        protected CanvasUpdateRegistry()
        {
            //Canvas中willRenderCanvases委托，会在Canvas.SendWillCanvases()中调用。
            //https://edu.uwa4d.com/lesson-detail/79/99/0?isPreview=0
            Canvas.willRenderCanvases += PerformUpdate;
        }

        /// <summary>
        /// Get the singleton registry instance.
        /// </summary>
        public static CanvasUpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new CanvasUpdateRegistry();
                return s_Instance;
            }
        }

        private bool ObjectValidForUpdate(ICanvasElement element)
        {
            var valid = element != null;

            var isUnityObject = element is Object;
            if (isUnityObject)
                valid = (element as Object) != null; //Here we make use of the overloaded UnityEngine.Object == null, that checks if the native object is alive.

            return valid;
        }


        //移除两个更新序列中，无效的item
        private void CleanInvalidItems()
        {
            // So MB's override the == operator for null equality, which checks
            // if they are destroyed. This is fine if you are looking at a concrete
            // mb, but in this case we are looking at a list of ICanvasElement
            // this won't forward the == operator to the MB, but just check if the
            // interface is null. IsDestroyed will return if the backend is destroyed.

            for (int i = m_LayoutRebuildQueue.Count - 1; i >= 0; --i)
            {
                var item = m_LayoutRebuildQueue[i];
                if (item == null)
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    item.LayoutComplete();
                }
            }

            for (int i = m_GraphicRebuildQueue.Count - 1; i >= 0; --i)
            {
                var item = m_GraphicRebuildQueue[i];
                if (item == null)
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    item.GraphicUpdateComplete();
                }
            }
        }

        //根据，obj往上有多少个父节点，进行排序（由少到多）
        //其实是，布局深度（深度：A/B/C/D/../E）
        private static readonly Comparison<ICanvasElement> s_SortLayoutFunction = SortLayoutList;

        /// <summary>
        ///这里需要思考的是，有可能一个Image对象，RectTransform和Graphics同时发生了修改，它们的更新含义不同需要区分对待
        ///1.修改了Image的宽高，这样Mesh的顶点会发生变化，此时该对象会加入m_LayoutRebuildQueue队列
        ///2.修改了Image的Sprite，它并不会影响顶点位置信息，此时该对象会加入m_GraphicRebuildQueue队列
        ///所以上面代码在遍历的时候会分层
        ////for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
        ////for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
        ///Rebuild的时候会把层传进去，保证Image知道现在是要更新布局，还是只更新渲染。
        /// </summary>
        private void PerformUpdate()
        {
            //开始BeginSample()
            //在Profiler中看到的标志性函数Canvas.willRenderCanvases耗时就在这里了
            //EndSample()
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
            CleanInvalidItems();//从两个序列中，删除不可用元素
            //（1）布局更新开始
            m_PerformingLayoutUpdate = true;

            m_LayoutRebuildQueue.Sort(s_SortLayoutFunction);
            //分别以PreLayout,Layout,PostLayout的参数顺序调用每一个元素的Rebuild方法
            for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
            {
                for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
                {
                    var rebuild = instance.m_LayoutRebuildQueue[j];
                    try
                    {
                        if (ObjectValidForUpdate(rebuild))
                            rebuild.Rebuild((CanvasUpdate)i);//重建布局元素
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, rebuild.transform);
                    }
                }
            }

            //调用布局更新完成的接口
            for (int i = 0; i < m_LayoutRebuildQueue.Count; ++i)
                m_LayoutRebuildQueue[i].LayoutComplete();

            instance.m_LayoutRebuildQueue.Clear();
            m_PerformingLayoutUpdate = false;

            // now layout is complete do culling...
            // 布局构建结束，开始进行Mask2D裁切（详细内容下面会介绍）
            ClipperRegistry.instance.Cull(); 

            //（2）图形更新开始
            m_PerformingGraphicUpdate = true;
            //以PreRender，LatePreRender的参数顺序调用每一个元素的Rebulid方法
            for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
            {
                for (var k = 0; k < instance.m_GraphicRebuildQueue.Count; k++)
                {
                    try
                    {
                        var element = instance.m_GraphicRebuildQueue[k];
                        if (ObjectValidForUpdate(element))
                            element.Rebuild((CanvasUpdate)i);//重建UI元素
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, instance.m_GraphicRebuildQueue[k].transform);
                    }
                }
            }

            //调用图形更新成功的接口
            for (int i = 0; i < m_GraphicRebuildQueue.Count; ++i)
                m_GraphicRebuildQueue[i].GraphicUpdateComplete();

            instance.m_GraphicRebuildQueue.Clear();
            m_PerformingGraphicUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
        }

        //计算child向上有个多少个父节点
        //其实就是计算节点的深度
        private static int ParentCount(Transform child)
        {
            if (child == null)
                return 0;

            var parent = child.parent;
            int count = 0;
            while (parent != null)
            {
                count++;
                parent = parent.parent;
            }
            return count;
        }

        private static int SortLayoutList(ICanvasElement x, ICanvasElement y)
        {
            Transform t1 = x.transform;
            Transform t2 = y.transform;

            return ParentCount(t1) - ParentCount(t2);
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// Will not return if successfully added.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        public static void RegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// <returns>
        /// True if the element was successfully added to the rebuilt list.
        /// False if either already inside a Graphic Update loop OR has already been added to the list.
        /// </returns>
        /// 相对上一个函数，多了一个返回“操作结果”的过程
        public static bool TryRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_LayoutRebuildQueue.Contains(element))
                return false;

            /* TODO: this likely should be here but causes the error to show just resizing the game view (case 739376)
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for layout rebuild while we are already inside a layout rebuild loop. This is not supported.", element));
                return false;
            }*/

            return m_LayoutRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// Try and add the given element to the rebuild list.
        /// Will not return if successfully added.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        public static void RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// Try and add the given element to the rebuild list.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// <returns>
        /// True if the element was successfully added to the rebuilt list.
        /// False if either already inside a Graphic Update loop OR has already been added to the list.
        /// </returns>
        public static bool TryRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported.", element));
                return false;
            }

            return m_GraphicRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// Remove the given element from both the graphic and the layout rebuild lists.
        /// </summary>
        /// <param name="element"></param>
        public static void UnRegisterCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }

        private void InternalUnRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }

            element.LayoutComplete();
            instance.m_LayoutRebuildQueue.Remove(element);
        }

        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }
            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.Remove(element);
        }

        /// <summary>
        /// Are graphics layouts currently being calculated..
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.Prelayout, CanvasUpdate.Layout or CanvasUpdate.Postlayout</returns>
        public static bool IsRebuildingLayout()
        {
            return instance.m_PerformingLayoutUpdate;
        }

        /// <summary>
        /// Are graphics currently being rebuild.
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.PreRender or CanvasUpdate.Render</returns>
        public static bool IsRebuildingGraphics()
        {
            return instance.m_PerformingGraphicUpdate;
        }
    }
}
