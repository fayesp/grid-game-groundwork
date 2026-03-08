using UnityEngine;
using Assets.Scripts;

public class OutletEnd : Pipe
{
	public override bool CanMoveToward(ref Vector3 MoveV3,Direction dir) {
        return false;
    }
}
