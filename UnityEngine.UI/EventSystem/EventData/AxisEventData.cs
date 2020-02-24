/****************************************************
    文件：AxisEventData.cs
    作者：JiahaoWu
    邮箱: jiahaodev@163.ccom
    日期：2020/02/24 17:25       
    功能：轴方向事件数据
         (多用于IMoveHandler接口中的OnMove方法）
*****************************************************/
namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Event Data associated with Axis Events (Controller / Keyboard).
    /// </summary>
    public class AxisEventData : BaseEventData
    {
        /// <summary>
        /// Raw input vector associated with this event.
        /// </summary>
        public Vector2 moveVector { get; set; }

        /// <summary>
        /// MoveDirection for this event.
        /// </summary>
        /// 【枚举】left、up、right、down、none
        public MoveDirection moveDir { get; set; }

        public AxisEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
            moveVector = Vector2.zero;
            moveDir = MoveDirection.None;
        }
    }
}
