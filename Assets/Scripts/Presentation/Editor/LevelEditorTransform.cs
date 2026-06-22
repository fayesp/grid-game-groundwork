using UnityEngine;

public partial class LevelEditor {

    #region 关卡变换

    /// <summary>
    /// 旋转关卡
    /// </summary>
    void RotateLevel(int degrees) {
    	currentLevelParent.transform.eulerAngles += new Vector3 (0,0,degrees);
		isDirty = true;
    }

    /// <summary>
    /// 翻转关卡
    /// </summary>
    void InvertLevel(string axis) {
    	foreach (Transform child in currentLevelParent.transform) {
			Vector3 p = child.position;
			Vector3 s = child.localScale;
			if (axis == "x") {
				child.position = new Vector3(-p.x, p.y, p.z);
				child.localScale = new Vector3(-s.x, s.y, s.z);
			} else {
				child.position = new Vector3(p.x, -p.y, p.z);
				child.localScale = new Vector3(s.x, -s.y, s.z);
			}
    	}
		isDirty = true;
    }

    #endregion
}
