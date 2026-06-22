using UnityEditor;
using UnityEngine;

public partial class LevelEditor {

    #region 场景视图交互

    /// <summary>
    /// 场景视图GUI事件
    /// </summary>
    public void SceneGUI(SceneView sceneView) {

		if (Utils.isMetaScene) return;
		if (currentLevelParentRef == null) return;

		e = Event.current;
		in2DMode = sceneView.in2DMode;

		if (e.modifiers != EventModifiers.None) {
			isHoldingAlt = true;
			mouseButtonDown = false;
		} else {
			isHoldingAlt = false;
		}

		Vector3 currentPos = GetPosition(e.mousePosition);
		if (selGridInt != 1) {
			currentPos += (Vector3.back * spawnHeight);
			currentPos = Utils.AvoidIntersect(currentPos);
		}

		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        var controlID = GUIUtility.GetControlID(FocusType.Passive);
		var eventType = e.GetTypeForControl(controlID);

		if (SceneView.mouseOverWindow != sceneView) {
			Reset();
		}
    	if (e.isKey && e.keyCode == KeyCode.P) {
    		EditorApplication.ExecuteMenuItem("Edit/Play");
    	}

    	if (isHoldingAlt) {
			if (eventType == EventType.ScrollWheel) {
				int deltaY = (e.delta.y < 0) ? -1 : 1;
				spawnHeight += deltaY;
				currentPos += (Vector3.back * deltaY);
				e.Use();
			}

		} else {

			if (eventType == EventType.MouseUp) {
				mouseButtonDown = false;
			}

			if (eventType == EventType.MouseDown) {

				if (e.button == 0 && selGridInt != 0) {
					e.Use();
					Refresh();
					drawPos = currentPos;
					CreateObject(Utils.Vec3ToInt(drawPos));
					mouseButtonDown = true;
					mousePosOnClick = e.mousePosition;

				} else if (e.button == 1) {
					selGridInt = 1;
					Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
					RaycastHit hit = new RaycastHit();

					if (Physics.Raycast(ray, out hit, 1000.0f)) {
						for (int i = 0; i < prefabs.Length; i++) {
							if (prefabs[i].transform.name == hit.transform.parent.name) {
								selGridInt = i + 2;
							}
						}
					}
				}

			} else if (mouseButtonDown) {

				if (Vector2.Distance(mousePosOnClick, e.mousePosition) > 10f) {
					if (!Utils.VectorRoughly2D(drawPos, currentPos, 0.75f)) {
						drawPos = Utils.Vec3ToInt(currentPos);
						CreateObject(drawPos);
						mousePosOnClick = e.mousePosition;
					}
				}
			}
    	}

		LevelGizmo.UpdateGizmo(currentPos, gizmoColor);
		LevelGizmo.Enable(selGridInt != 0);
		sceneView.Repaint();
		Repaint();
    }

    /// <summary>
    /// 根据鼠标位置获取世界坐标
    /// </summary>
    Vector3 GetPosition(Vector3 mousePos) {
		if (in2DMode) {
			Vector3 screenPosition = HandleUtility.GUIPointToWorldRay(mousePos).origin;
			return Utils.Vec3ToInt(new Vector3(screenPosition.x, screenPosition.y, 0));
		} else {
			Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

         	RaycastHit hit = new RaycastHit();
			if (Physics.Raycast(ray, out hit, 100.0f)) {
				Vector3 pos = hit.point + (hit.normal * 0.5f);
				if (selGridInt == 1) {
					pos = hit.transform.position;
				}
				return Utils.Vec3ToInt(pos);
			}

			Plane hPlane = new Plane(Vector3.forward, Vector3.zero);
   			float distance = 0;
			if (hPlane.Raycast(ray, out distance)){
				return Utils.Vec3ToInt(ray.GetPoint(distance));
   			}
		}
		return Vector3.zero;
    }

	/// <summary>
	/// 通过名称获取预制体
	/// </summary>
	GameObject GetPrefabByName(string s) {
		foreach (GameObject prefab in prefabs) {
			if (prefab.transform.name.Contains(s)) {
				return prefab;
			}
		}
		return null;
	}

    /// <summary>
    /// 创建对象或擦除对象
    /// </summary>
    void CreateObject(Vector3 pos) {

		if (selGridInt == 1) {
			ClearObjectsAtPosition(Vector3Int.RoundToInt(pos));

		} else {
			GameObject prefab = prefabs[selGridInt - 2];

			newGameObject = PrefabUtility.InstantiatePrefab(prefab as GameObject) as GameObject;
			newGameObject.transform.position = pos;
			newGameObject.transform.parent = currentLevelParent.transform;

			int z = 0;
			switch (rotateInt) {
				case 0:
					z = 0;
					break;
				case 1:
					z = 90;
					break;
				case 2:
					z = 180;
					break;
				case 3:
					z = 270;
					break;
			}

			newGameObject.transform.eulerAngles = new Vector3(0,0,z);

			Vector3 p = newGameObject.transform.position;
			if (spawnHeight < p.z) {
				newGameObject.transform.position = new Vector3(p.x, p.y, -Mathf.Abs(spawnHeight));
			}

			Utils.AvoidIntersect(newGameObject.transform);

			if (playModeActive) {
				LevelPlayModePersistence.SaveNewObject(newGameObject);
			}

        	Undo.RegisterCreatedObjectUndo (newGameObject, "Create object");
		}

        Refresh();

		isDirty = true;
    }

    /// <summary>
    /// 擦除指定位置的对象
    /// </summary>
    void ClearObjectsAtPosition(Vector3Int pos) {

		bool foundSomething = true;
		while (foundSomething) {
			foundSomething = false;
			foreach (Transform child in currentLevelParent.transform) {
				Transform target = GetTarget(child);
				foreach (Transform tile in target) {
					bool atPosition = (in2DMode) ? Utils.VectorRoughly2D(tile.position, pos) : Utils.VectorRoughly(tile.position, pos);
					if (tile.CompareTag("Tile") && atPosition) {
						foundSomething = true;
						Undo.DestroyObjectImmediate(child.gameObject);
						break;
					}
				}
			}
		}
		isDirty = true;
    }

	/// <summary>
	/// 获取目标Transform（如Extender特殊处理）
	/// </summary>
	Transform GetTarget(Transform t) {
		if (t.name.Contains("Extender")) {
			return t.GetComponentInChildren<Wall>().transform;
		}
		return t;
	}

    #endregion
}
