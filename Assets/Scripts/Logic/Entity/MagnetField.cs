using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;


namespace Assets.Scripts
{
    public class MagnetField : MonoBehaviour
    {
        //public Transform MagnetTransform;
        private MagnetType MagnetType; // 磁铁类型
        //public Vector3 Direction;
        private Magnet Magnet;
        private List<Mover> moversInField = new List<Mover>();

        void Start()
        {
            Magnet = GetComponentInParent<Magnet>();
            MagnetType = Magnet.MagnetType; // 获取磁铁类型
        }

        // 当其他对象进入触发器范围时调用
        //磁场相重叠会不会触发?
        private void OnTriggerEnter(Collider other)
        {
            //todo 刚进入的时候添加到moversInField
            Mover mover = other.GetComponent<Mover>();
            moversInField.Add(mover);
            //getMagnetForce应该要到移动后的地方再,计算磁力移动的距离

        }

        // 当其他对象离开触发器范围时调用
        private void OnTriggerExit(Collider other)
        {
            //去除attach的磁块
            Magnet.RemoveAttach(other.transform);
            other.GetComponent<Magnet>().RemoveAttach(transform);

            //离开移除inField
            RemoveInField(other);
        }


        public void PlanMagnetEffect()
        {
            foreach (var mover in moversInField)
            {
                Vector3 MForceMove = GetMagnetForce(Magnet.transform, mover.transform);
                Direction moveDirection = Utils.CheckDirection(MForceMove);
                mover.TryPlanMove(MForceMove, moveDirection);
            }
        }

        public Vector3 GetMagnetForce(Transform masterMagnet, Transform slaveMagnet)
        {
            //todo 如果是铁块的磁力如何判断?
            //判断tag是否为magnet
            if (!masterMagnet.gameObject.CompareTag("Magent") || !slaveMagnet.gameObject.CompareTag("Magnet"))
            {
                return Vector3.zero;
            }

            //获取magnetID
            MagnetType MMagnetType = masterMagnet.GetComponent<MagnetField>().MagnetType;
            MagnetType SMagnetType = slaveMagnet.GetComponent<MagnetField>().MagnetType;

            //获取磁力方向
            Vector3 Dir = slaveMagnet.position - masterMagnet.position;
            Vector3 FDir = new Vector3(RoundToOne(Dir.x), RoundToOne(Dir.y), RoundToOne(Dir.z));
            //不考虑反作用力
            Vector3 FDirReverse = new Vector3(-FDir.x, -FDir.y, -FDir.z);

            //先区分单双极面
            PolarType masterPolar = GetPolar(MMagnetType, FDir);
            PolarType slavePolar = GetPolar(SMagnetType, FDirReverse);


            int ForceType = 0;
            //然后通过direction判断磁力
            switch (masterPolar)
            {
                case PolarType.S:
                    ForceType = SCheck(slavePolar, Dir);
                    break;
                case PolarType.N:
                    ForceType = NCheck(slavePolar, Dir);
                    break;
                case PolarType.XPS:
                    ForceType = XPSCheck(slavePolar, Dir);
                    break;
                case PolarType.XPN:
                    ForceType = XPNCheck(slavePolar, Dir);
                    break;
                case PolarType.YPS:
                    ForceType = YPSCheck(slavePolar, Dir);
                    break;
                case PolarType.YPN:
                    ForceType = YPNCheck(slavePolar, Dir);
                    break;
                case PolarType.ZPS:
                    ForceType = ZPSCheck(slavePolar, Dir);
                    break;
                case PolarType.ZPN:
                    ForceType = ZPNCheck(slavePolar, Dir);
                    break;
                default:
                    break;
            }



            switch (ForceType)
            {
                case 0: // 平衡状态
                    break;
                case 1: // 吸引力 添加attcch物体
                    masterMagnet.GetComponent<Magnet>().AddAttach(slaveMagnet);
                    slaveMagnet.GetComponent<Magnet>().AddAttach(masterMagnet);
                    break;
                case -1: // 排斥力
                    //Vector3Int RepelMove = Vector3Int.RoundToInt(FDir);
                    //todo 重新规划
                    //GuestMagnet.GetComponent<Magnet>().PlanMove(RepelMove,);
                    break;
                default:
                    break;
            }

            return MoveCalculate(Dir, FDir, ForceType);

        }



        /// <summary>
        /// 计算磁力作用下的位移
        /// </summary>
        /// <param name="Dir">磁块连线向量</param>
        /// <param name="FDir">磁力方向</param>
        /// <param name="ForceTyep">磁力类型</param>
        /// <returns>位移向量</returns>
        public Vector3 MoveCalculate(Vector3 Dir, Vector3 FDir, int ForceTyep)
        {
            switch (ForceTyep)
            {
                case 0:
                    return Vector3.zero;
                case 1:
                    return -Vector3.Dot(Dir, FDir) * FDir;
                case -1:
                    return FDir - Vector3.Dot(Dir, FDir) * FDir;
                default:
                    break;
            }

            return new Vector3(0, 0, 0);

        }

        /// <summary>
        /// 将向量,规整为受力方向为-1/1,不受力为0
        /// </summary>
        /// <param name="number"></param>
        /// <returns>绝对值小于1返回0,大于1返回1/-1</returns>
        public int RoundToOne(float number)
        {
            if (Math.Abs(number) < 1)
                return 0;
            //todo 悬浮的状态如何判定
            if (Math.Abs(number) > 1)
            {
                if (number > 0)
                    return 1;
                else
                    return -1;
            }
            else
            {
                return Convert.ToInt32(number);
            }
        }
        /// <summary>
        /// 依据磁铁类型和受力方向获取磁极类型
        /// </summary>
        /// <param name="magnetType"></param>
        /// <param name="FrwdDir"></param>
        /// <returns>受力面磁极类型</returns>
        public PolarType GetPolar(MagnetType magnetType, Vector3 FrwdDir)
        {
            //使用>或<0来判断还是== 1/-1来判断?
            switch (magnetType)
            {
                case MagnetType.XP:
                    if (FrwdDir.x > 0)
                        return PolarType.S;
                    else if (FrwdDir.x < 0)
                        return PolarType.N;
                    else
                        return PolarType.XPS;
                case MagnetType.XN:
                    if (FrwdDir.x > 0)
                        return PolarType.N;
                    else if (FrwdDir.x < 0)
                        return PolarType.S;
                    else
                        return PolarType.XPN;
                case MagnetType.YP:
                    if (FrwdDir.y > 0)
                        return PolarType.S;
                    else if (FrwdDir.y < 0)
                        return PolarType.N;
                    else
                        return PolarType.YPS;
                case MagnetType.YN:
                    if (FrwdDir.y > 0)
                        return PolarType.N;
                    else if (FrwdDir.y < 0)
                        return PolarType.S;
                    else
                        return PolarType.YPN;
                case MagnetType.ZP:
                    if (FrwdDir.z > 0)
                        return PolarType.S;
                    else if (FrwdDir.z < 0)
                        return PolarType.N;
                    else
                        return PolarType.ZPS;
                case MagnetType.ZN:
                    if (FrwdDir.z > 0)
                        return PolarType.N;
                    else if (FrwdDir.z < 0)
                        return PolarType.S;
                    else
                        return PolarType.ZPN;
                default:
                    return PolarType.None; // 如果没有匹配的类型，返回None
            }
        }

        #region different Polar magnet force
        /// <summary>
        /// S极的检查,判断与guestPolar的磁力作用
        /// </summary>
        /// <param name="guestPolar"></param>
        /// <param name="direction"></param>
        /// <returns>0表示balance,1表示引力,-1表示斥力</returns>
        public int SCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    return -1; // 同极相斥
                case PolarType.N:
                    return 1; // 异极相吸
                case PolarType.XPS:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return 1; // 单极对双极相吸
                    else
                        return -1; // 单极对双极相斥
                case PolarType.XPN:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return -1; // 单极对双极相斥
                    else
                        return 1; // 单极对双极相吸
                case PolarType.YPS:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return 1; // 单极对双极相吸
                    else
                        return -1; // 单极对双极相斥
                case PolarType.YPN:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return -1; // 单极对双极相斥
                    else
                        return 1; // 单极对双极相吸
                case PolarType.ZPS:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return 1; // 单极对双极相吸
                    else
                        return -1; // 单极对双极相斥
                case PolarType.ZPN:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return -1; // 单极对双极相斥
                    else
                        return 1; // 单极对双极相吸
                default:
                    return 0;
            }
        }

        public int NCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    return 1;
                case PolarType.N:
                    return -1;
                case PolarType.XPS:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return -1; // 单极对双极相斥
                    else
                        return 1; // 单极对双极相吸
                case PolarType.XPN:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return 1; // 单极对双极相吸
                    else
                        return -1; // 单极对双极相斥
                case PolarType.YPS:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return -1; // 单极对双极相斥
                    else
                        return 1; // 单极对双极相吸
                case PolarType.YPN:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return 1; // 单极对双极相吸
                    else
                        return -1; // 单极对双极相斥
                case PolarType.ZPS:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return -1; // 单极对双极相斥
                    else
                        return 1; // 单极对双极相吸
                case PolarType.ZPN:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return 1; // 单极对双极相吸
                    else
                        return -1; // 单极对双极相斥
                default:
                    return 0;
            }
        }

        public int XPSCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return -1; // 单极对双极相吸
                    else
                        return 1; // 单极对双极相斥
                case PolarType.N:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return 1; // 单极对双极相斥
                    else
                        return -1; // 单极对双极相吸
                case PolarType.XPS:
                    //todo 增加x轴的变化,似乎不太可能平衡,数值计算需要思考1/3
                    if (Math.Abs(direction.x) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.x) < 0.333f)
                        return -1; // 同极相斥
                    else
                        return 1;
                case PolarType.XPN:
                    if (Math.Abs(direction.x) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.x) < 0.333f)
                        return 1; // 同极相斥
                    else
                        return -1;
                case PolarType.YPS:
                case PolarType.YPN:
                case PolarType.ZPS:
                case PolarType.ZPN:
                default:
                    return 0;
            }
        }

        public int XPNCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return 1; // 单极对双极相斥
                    else
                        return -1; // 单极对双极相吸
                case PolarType.N:
                    if (direction.x == 0f)
                        return 0;
                    else if (direction.x > 0f)
                        return -1; // 单极对双极相吸
                    else
                        return 1; // 单极对双极相斥
                case PolarType.XPS:
                    if (Math.Abs(direction.x) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.x) < 0.333f)
                        return 1; // 同极相斥
                    else
                        return -1;
                case PolarType.XPN:
                    //todo 增加x轴的变化,似乎不太可能平衡,数值计算需要思考1/3
                    if (Math.Abs(direction.x) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.x) < 0.333f)
                        return -1; // 同极相斥
                    else
                        return 1;
                case PolarType.YPS:
                case PolarType.YPN:
                case PolarType.ZPS:
                case PolarType.ZPN:
                default:
                    return 0;
            }
        }

        public int YPSCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return -1; // 单极对双极相吸
                    else
                        return 1; // 单极对双极相斥
                case PolarType.N:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return 1; // 单极对双极相斥
                    else
                        return -1; // 单极对双极相吸
                case PolarType.XPS:
                case PolarType.XPN:
                case PolarType.YPS:
                    //todo 增加y轴的变化,似乎不太可能平衡,数值计算需要思考1/3
                    if (Math.Abs(direction.y) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.y) < 0.333f)
                        return -1; // 同极相斥
                    else
                        return 1;
                case PolarType.YPN:
                    if (Math.Abs(direction.y) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.y) < 0.333f)
                        return 1; // 同极相斥
                    else
                        return -1;
                case PolarType.ZPS:
                case PolarType.ZPN:
                default:
                    return 0;
            }
        }

        public int YPNCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return 1; // 单极对双极相斥
                    else
                        return -1; // 单极对双极相吸
                case PolarType.N:
                    if (direction.y == 0f)
                        return 0;
                    else if (direction.y > 0f)
                        return -1; // 单极对双极相吸
                    else
                        return 1; // 单极对双极相斥
                case PolarType.XPS:
                case PolarType.XPN:
                case PolarType.YPS:
                    if (Math.Abs(direction.y) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.y) < 0.333f)
                        return 1;
                    else
                        return -1;
                case PolarType.YPN:
                    //todo 增加y轴的变化,似乎不太可能平衡,数值计算需要思考1/3
                    if (Math.Abs(direction.y) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.y) < 0.333f)
                        return -1; // 同极相斥
                    else
                        return 1;
                case PolarType.ZPS:
                case PolarType.ZPN:
                default:
                    return 0;
            }
        }

        public int ZPSCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return -1; // 单极对双极相吸
                    else
                        return 1; // 单极对双极相斥
                case PolarType.N:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return 1; // 单极对双极相斥
                    else
                        return -1; // 单极对双极相吸
                case PolarType.XPS:
                case PolarType.XPN:
                case PolarType.YPS:
                case PolarType.YPN:
                case PolarType.ZPS:
                    //todo 增加z轴的变化,似乎不太可能平衡,数值计算需要思考1/3
                    if (Math.Abs(direction.z) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.z) < 0.333f)
                        return -1; // 同极相斥
                    else
                        return 1;
                case PolarType.ZPN:
                    if (Math.Abs(direction.z) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.z) < 0.333f)
                        return 1; // 同极相斥
                    else
                        return -1;
                default:
                    return 0;
            }
        }

        public int ZPNCheck(PolarType guestPolar, Vector3 direction)
        {
            switch (guestPolar)
            {
                case PolarType.None:
                    return 0;
                case PolarType.S:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return 1; // 单极对双极相吸
                    else
                        return -1; // 单极对双极相斥
                case PolarType.N:
                    if (direction.z == 0f)
                        return 0;
                    else if (direction.z > 0f)
                        return -1; // 单极对双极相斥
                    else
                        return 1; // 单极对双极相吸
                case PolarType.XPS:
                case PolarType.XPN:
                case PolarType.YPS:
                case PolarType.YPN:
                case PolarType.ZPS:
                    //todo 增加z轴的变化,似乎不太可能平衡,数值计算需要思考1/3
                    if (Math.Abs(direction.z) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.z) < 0.333f)
                        return 1; // 同极相斥
                    else
                        return -1;
                case PolarType.ZPN:
                    if (Math.Abs(direction.z) == 0.333f)
                        return 0;
                    else if (Math.Abs(direction.z) < 0.333f)
                        return -1; // 同极相斥
                    else
                        return 1;
                default:
                    return 0;
            }
        }

        #endregion different Polar check

        #region remove
        public void RemoveInField(Collider other)
        {
            Mover mover = other.GetComponent<Mover>();
            if (mover != null && moversInField.Contains(mover))
            {
                moversInField.Remove(mover);
            }
        }
        #endregion remove

    }

}