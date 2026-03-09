using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Assets.Scripts
{
    public class Magnet : Mover
    {
        public bool isMagnet
        {
            get => this.gameObject.CompareTag("Magnet");
        }
        [HideInInspector]public bool IsBlock;
        public MagnetType MagnetType;
        public MagnetField[] MagnetFields
        {
            get
            {
                return GetComponents<MagnetField>();
            }
        }
        [HideInInspector]public bool AttachMoved = false;
        public void AddAttach(Transform magnet)
        {
            if (magnet == null || AttachBlock.Contains(magnet))
                return;
            AttachBlock.Add(magnet);
        }
        public void RemoveAttach(Transform magnet)
        {
            if (magnet == null)
                return;
            if (AttachBlock.Contains(magnet))
            {
                AttachBlock.Remove(magnet);
            }
        }
        /// <summary>
        /// 引力相连的磁块联动
        /// </summary>
        /// <param name="MoveV3"></param>
        /// <returns></returns>
        public override bool TryPlanMove(Vector3 MoveV3, Direction Dir)
        {
            HashSet<Mover> visited = new HashSet<Mover>();
            if (!CanMoveToward(ref MoveV3, Dir, visited))
                return false;
            AttachMoved = true;
            visited.Clear();
            PlanMove(MoveV3, Dir, visited);
            foreach (Transform t in AttachBlock)
            {
                bool IsAttachMoved = t.GetComponent<Magnet>().AttachMoved;
                if (!IsAttachMoved)
                {
                    t.GetComponent<Magnet>().TryPlanMove(MoveV3, Dir);
                }
            }
            AttachMoved = false;
            return true;
        }
        //todo 待定磁块的连动如何编写?
        public override void PlanMove(Vector3 MoveV3, Direction Dir, HashSet<Mover> visited = null)
        {
            if (visited == null)
                visited = new HashSet<Mover>();

            //防止强迫推动?加的判断,
            //防止自己推动自己,两个U形,u形加单块,死循环
            if (!CanMoveToward(ref MoveV3, Dir, visited))
                return;
            if (PlannedMove == MoveV3)
                return;

            // Prevent infinite recursion
            if (visited.Contains(this))
                return;
            visited.Add(this);

            PlannedMove = MoveV3;
            PlanPushes(MoveV3, Dir, visited);
            AttachMoved = true;
            foreach (Transform t in AttachBlock)
            {
                bool IsAttachMoved = t.GetComponent<Magnet>().AttachMoved;
                if (!IsAttachMoved)
                {
                    t.GetComponent<Magnet>().PlanMove(MoveV3, Dir, visited);
                }
            }
            AttachMoved = false;
        }
        public override void DoPostMoveEffects()
        {
            DoMagnetEffects();
            if (ShouldFall())
                PlanMove(Utils.forward, Direction.Forward);
        }
        public void DoMagnetEffects()
        {
            foreach (var field in MagnetFields)
            {
                field.PlanMagnetEffect();
            }
        }
    }
}