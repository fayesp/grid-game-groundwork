using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Assets.Scripts
{
    public class CubeRoller : MonoBehaviour
    {
        [Header("移动设置")]
        public float moveSpeed = 5f;          // 移动速度
        public float rollDuration = 0.5f;     // 翻滚持续时间
        [Header("地面检测")]
        public LayerMask groundLayer;         // 地面层级
        public float groundCheckDistance = 0.6f; // 地面检测距离
        private bool isRolling = false;       // 是否正在翻滚
        private Vector3 rollDirection;        // 翻滚方向
        private float rollProgress = 0f;      // 翻滚进度
        private Vector3 rollPivot;            // 翻滚支点
        private Quaternion rollStartRotation; // 翻滚起始旋转
        private void Update()
        {
            if (!isRolling)
            {
                HandleInput();
            }
            else
            {
                ContinueRoll();
            }
        }
        // 处理玩家输入
        private void HandleInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            // 确保只朝一个方向移动
            if (Mathf.Abs(horizontal) > 0.1f && Mathf.Abs(vertical) > 0.1f)
            {
                // 如果同时按下两个方向键，优先水平方向
                vertical = 0;
            }
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                StartRoll(horizontal > 0 ? Vector3.right : Vector3.left);
            }
            else if (Mathf.Abs(vertical) > 0.1f)
            {
                StartRoll(vertical > 0 ? Vector3.forward : Vector3.back);
            }
        }
        // 开始翻滚
        private void StartRoll(Vector3 direction)
        {
            // 检查目标位置是否可行走
            if (!CanRollTo(direction))
                return;
            isRolling = true;
            rollDirection = direction;
            rollProgress = 0f;
            // 计算翻滚支点（立方体边缘）
            CalculateRollPivot(direction);
            // 保存起始旋转
            rollStartRotation = transform.rotation;
        }
        // 计算翻滚支点
        private void CalculateRollPivot(Vector3 direction)
        {
            // 立方体的一半尺寸
            float halfSize = transform.localScale.x / 2f;
            // 计算支点位置（当前立方体底部边缘）
            Vector3 bottomCenter = transform.position - Vector3.up * halfSize;
            rollPivot = bottomCenter + (direction * halfSize);
            // 可视化调试（可选）
            Debug.DrawLine(transform.position, rollPivot, Color.red, 1f);
        }
        // 修正后的ContinueRoll方法
        private void ContinueRoll()
        {
            rollProgress += Time.deltaTime / rollDuration;
            if (rollProgress < 1f)
            {
                // 计算当前旋转角度（0-90度）
                float angle = 90f * rollProgress;
                // 创建绕支点旋转的变换
                Vector3 rotationAxis = GetRotationAxis(rollDirection);
                transform.RotateAround(rollPivot, rotationAxis, 90f * Time.deltaTime / rollDuration);
            }
            else
            {
                CompleteRoll();
            }
        }
        // 获取正确的旋转轴,坐标系的不同导致会不同的旋转轴的返回
        //todo 坐标的基准不统一,水平面是x,y,竖直方向是z使用左手定则
        private Vector3 GetRotationAxis(Vector3 direction)
        {
            if (direction == Vector3.forward)
                return Vector3.right;
            if (direction == Vector3.back)
                return Vector3.left;
            if (direction == Vector3.right)
                return Vector3.back;
            if (direction == Vector3.left)
                return Vector3.forward;
            return Vector3.zero;
        }
        // 修正CompleteRoll方法
        private void CompleteRoll()
        {
            // 精确设置最终位置 y= 0.5表示在地面上
            Vector3 finalPosition = transform.position;
            finalPosition.y = transform.localScale.y / 2f; // 确保在地面上
            transform.position = finalPosition;
            // 对齐到90度倍数，避免浮点误差
            Vector3 euler = transform.eulerAngles;
            euler.x = Mathf.Round(euler.x / 90) * 90;
            euler.y = Mathf.Round(euler.y / 90) * 90;
            euler.z = Mathf.Round(euler.z / 90) * 90;
            transform.eulerAngles = euler;
            isRolling = false;
        }
        // 检查是否可以翻滚到目标位置
        private bool CanRollTo(Vector3 direction)
        {
            // 计算目标位置
            Vector3 targetPosition = transform.position + direction * transform.localScale.x;
            // 检查目标位置是否有障碍物
            if (Physics.CheckSphere(targetPosition, transform.localScale.x / 4f, groundLayer))
            {
                return false;
            }
            // 检查目标位置是否有地面支撑
            RaycastHit hit;
            if (!Physics.Raycast(targetPosition + Vector3.up * 0.5f, Vector3.down, out hit, 1f, groundLayer))
            {
                return false;
            }
            return true;
        }
        // 可视化调试
        private void OnDrawGizmos()
        {
            // 绘制地面检测范围
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, transform.localScale.x / 4f);
            // 绘制当前移动方向
            if (isRolling)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + rollDirection * 2f);
                // 绘制翻滚支点
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(rollPivot, 0.1f);
            }
        }
    }
}