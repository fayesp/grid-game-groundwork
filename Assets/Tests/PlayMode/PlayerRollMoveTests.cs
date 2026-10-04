using System.Collections;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Player 翻滚移动的行为测试。
/// 通过公共接口（InputBuffer + CheckBufferedInput）驱动完整的移动流程，
/// 观察 transform 位置与父层级这些可观测行为。
/// </summary>
public class PlayerRollMoveTests
{
    GameObject gameGo;
    GameObject container;
    GameObject playerGo;
    Player player;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        gameGo = new GameObject("Game");
        gameGo.AddComponent<Game>();

        // 非原点容器：验证翻滚/父级恢复全程使用世界坐标且层级还原正确
        container = new GameObject("Container");
        container.transform.position = new Vector3(5, 0, 0);

        playerGo = new GameObject("Player");
        playerGo.tag = "Player";
        playerGo.transform.SetParent(container.transform, false);
        playerGo.transform.localPosition = Vector3.zero;
        player = playerGo.AddComponent<Player>();

        // Movers/Walls 要求带 BoxCollider 且 tag 为 "Tile" 的子物体
        GameObject tileGo = new GameObject("Tile");
        tileGo.tag = "Tile";
        tileGo.transform.SetParent(playerGo.transform, false);
        tileGo.transform.localPosition = Vector3.zero;
        tileGo.AddComponent<BoxCollider>();

        // 等待 Game 初始化协程完成：movers 重新注册且引用有效（上一测试的残留已被销毁）
        int tries = 0;
        while ((Game.movers.Count == 0 || Game.movers[0] == null) && tries < 600)
        {
            if (tries % 60 == 0)
            {
                Debug.Log(string.Format(
                    "[TEST] 等待 movers 注册: count={0} game实例={1} game对象激活={2}",
                    Game.movers.Count, Game.instance != null, gameGo.activeInHierarchy));
            }
            tries++;
            yield return null;
        }
        Assert.Less(tries, 600, "Game.movers 注册超时（InitAfterFrame 协程未完成的帧数超过 600）");
        yield return null; // 让 InitAfterFrame 协程收尾（State.Init 等）
        Assert.AreEqual(1, Game.movers.Count, "初始化后应注册 1 个 Mover");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        DOTween.KillAll();
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(container);
        Object.DestroyImmediate(gameGo);
        Object.DestroyImmediate(GameObject.Find("GameServices"));
        // Player.Start 创建的 RollPivot 是独立根物体，需单独清理
        GameObject pivot = GameObject.Find("RollPivot");
        if (pivot != null)
        {
            Object.DestroyImmediate(pivot);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator RollRight_DuringRollPlayerLeavesStartCell()
    {
        player.InputBuffer.Add(Vector3Int.right);
        player.CheckBufferedInput();

        // 给 DOTween 两帧推进翻滚动画
        yield return null;
        yield return null;

        float x = player.transform.position.x;
        Assert.IsTrue(
            x > 5.05f && x < 6f,
            "翻滚动画应带动玩家离开起点，实际 x = " + x);
    }

    [UnityTest]
    public IEnumerator RollRight_AfterCompletion_PlayerLandsOnTargetCellAndParentRestored()
    {
        player.InputBuffer.Add(Vector3Int.right);
        player.CheckBufferedInput();

        // 等待翻滚 + 下落阶段全部动画完成
        yield return new WaitForSeconds(1.5f);

        Assert.AreEqual(
            new Vector3(6, 0, 0), player.transform.position,
            "翻滚完成后玩家应停在右侧一格");
        Assert.AreEqual(
            container.transform, player.transform.parent,
            "动画完成后父级应恢复为原始父物体");
        Assert.IsFalse(Game.instance.isMoving, "移动完成后不应再有动画进行");
    }

    [UnityTest]
    public IEnumerator RollRight_AfterCompletion_PlayerStaysPut_NoInfiniteFall()
    {
        player.InputBuffer.Add(Vector3Int.right);
        player.CheckBufferedInput();
        yield return new WaitForSeconds(1.5f);

        // 静默观察：不应发生任何后续下落/移动
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(new Vector3(6, 0, 0), player.transform.position);
        Assert.IsFalse(Game.instance.isMoving);
    }

    [UnityTest]
    public IEnumerator RollRightThenUp_SecondRollArcEndsNearTargetCell()
    {
        // 第一次：右移完成
        player.InputBuffer.Add(Vector3Int.right);
        player.CheckBufferedInput();
        yield return new WaitForSeconds(1.0f);

        // 第二次：向上翻滚（旋转轴改变）。
        // pivot 若保留上次旋转，第二次翻滚的合成旋转会偏到错误平面
        player.InputBuffer.Add(Vector3Int.up);
        player.CheckBufferedInput();

        // 在第二次翻滚后段采样：正确弧线终点应接近 (6,1,0)，且 x 恒为 6
        yield return new WaitForSeconds(0.12f);
        bool sawLateArcNearTarget = false;
        for (float t = 0; t < 0.09f; t += Time.deltaTime)
        {
            Vector3 p = player.transform.position;
            float distToTarget = Vector3.Distance(p, new Vector3(6, 1, 0));
            if (distToTarget < 0.35f && Mathf.Abs(p.x - 6f) < 0.05f)
            {
                sawLateArcNearTarget = true;
            }
            yield return null;
        }
        Assert.IsTrue(sawLateArcNearTarget, "第二次翻滚（换向）后段应接近目标格 (6,1,0) 且 x 不变");

        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(new Vector3(6, 1, 0), player.transform.position);
        Assert.IsFalse(Game.instance.isMoving);
    }

    /// <summary>
    /// 物理接地翻滚基准（ADR-0001）：绕支撑面前缘边翻倒——非运动的水平轴保持恒定，
    /// 中心沿 -Z 短暂抬起（约 0.207）。锁定右移：y 恒为 0、z 有抬升、终点 (6,0,0)。
    /// 屏幕面内翻滚（绕 Z 轴、y 恒定但无抬升、z 恒为 0）对本测试为红。
    /// </summary>
    [UnityTest]
    public IEnumerator RollRight_PhysicalTipOver_YConstant_ZLifts()
    {
        player.InputBuffer.Add(Vector3Int.right);
        player.CheckBufferedInput();

        float maxAbsY = 0f;
        float minZ = float.MaxValue;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            Vector3 p = player.transform.position;
            maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(p.y));
            minZ = Mathf.Min(minZ, p.z);
            yield return null;
        }
        Assert.LessOrEqual(maxAbsY, 0.05f, "右移翻滚 y 应保持为 0，实际 |y| = " + maxAbsY);
        Assert.Less(minZ, -0.15f, "右移翻滚中心应沿 -Z 抬起，实际 minZ = " + minZ);

        yield return new WaitForSeconds(1.3f);
        Assert.AreEqual(new Vector3(6, 0, 0), player.transform.position);
        Assert.AreEqual(container.transform, player.transform.parent);
        Assert.IsFalse(Game.instance.isMoving);
    }

    /// <summary>
    /// 上滚绕 +X（ADR-0001）：x 恒为 5、z 抬升、终点 (5,1,0)。
    /// 屏幕面内翻滚（绕 Z 轴、x 左右摆动）对本测试为红。
    /// </summary>
    [UnityTest]
    public IEnumerator RollUp_PhysicalTipOver_XConstant_ZLifts()
    {
        player.InputBuffer.Add(Vector3Int.up);
        player.CheckBufferedInput();

        float maxAbsX = 0f;
        float minZ = float.MaxValue;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            Vector3 p = player.transform.position;
            maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(p.x - 5f));
            minZ = Mathf.Min(minZ, p.z);
            yield return null;
        }
        Assert.LessOrEqual(maxAbsX, 0.05f, "上移翻滚 x 应保持为 5，实际 |x-5| = " + maxAbsX);
        Assert.Less(minZ, -0.15f, "上移翻滚中心应沿 -Z 抬起，实际 minZ = " + minZ);

        yield return new WaitForSeconds(1.3f);
        Assert.AreEqual(new Vector3(5, 1, 0), player.transform.position, "翻滚完成后玩家应停在上方一格");
        Assert.AreEqual(container.transform, player.transform.parent, "动画完成后父级应恢复为原始父物体");
        Assert.IsFalse(Game.instance.isMoving);
    }

    /// <summary>
    /// 下滚绕 -X（ADR-0001）：与上滚同构——x 恒为 5、z 抬升、终点 (5,-1,0)。
    /// </summary>
    [UnityTest]
    public IEnumerator RollDown_PhysicalTipOver_XConstant_ZLifts()
    {
        player.InputBuffer.Add(Vector3Int.down);
        player.CheckBufferedInput();

        float maxAbsX = 0f;
        float minZ = float.MaxValue;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            Vector3 p = player.transform.position;
            maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(p.x - 5f));
            minZ = Mathf.Min(minZ, p.z);
            yield return null;
        }
        Assert.LessOrEqual(maxAbsX, 0.05f, "下移翻滚 x 应保持为 5，实际 |x-5| = " + maxAbsX);
        Assert.Less(minZ, -0.15f, "下移翻滚中心应沿 -Z 抬起，实际 minZ = " + minZ);

        yield return new WaitForSeconds(1.3f);
        Assert.AreEqual(new Vector3(5, -1, 0), player.transform.position, "翻滚完成后玩家应停在下方一格");
        Assert.AreEqual(container.transform, player.transform.parent, "动画完成后父级应恢复为原始父物体");
        Assert.IsFalse(Game.instance.isMoving);
    }

    [UnityTest]
    public IEnumerator RollRightThenFall_FallMovesStraightDownOneCell()
    {
        // 场景：玩家在 (5,0,1) 高度，右移一格后下方 (6,0,2) 为空、(6,0,3) 有墙（地面）
        // → 水平翻滚到 (6,0,1) 后应恰好下落一格到 (6,0,2) 停住
        playerGo.transform.localPosition = new Vector3(0, 0, 1);
        GameObject wallGo = new GameObject("Wall");
        wallGo.transform.position = new Vector3(6, 0, 3);
        wallGo.AddComponent<Wall>();
        GameObject wallTile = new GameObject("Tile");
        wallTile.tag = "Tile";
        wallTile.transform.SetParent(wallGo.transform, false);
        wallTile.AddComponent<BoxCollider>();
        Game.instance.SyncGrid();

        player.InputBuffer.Add(Vector3Int.right);
        player.CheckBufferedInput();

        // 采样下落动画：下落期间玩家应直线下降（x 恒为 6），
        // 若误用水平翻滚的陈旧旋转轴，玩家会横向甩出（x 偏移到 6.1+）
        bool sawDeepDescent = false;
        bool xStableDuringDescent = true;
        for (float t = 0; t < 0.7f; t += Time.deltaTime)
        {
            Vector3 p = player.transform.position;
            if (p.z > 1.5f)
            {
                sawDeepDescent = true;
            }
            if (p.z > 1.1f && Mathf.Abs(p.x - 6f) > 0.05f)
            {
                xStableDuringDescent = false;
            }
            yield return null;
        }

        Assert.IsTrue(sawDeepDescent, "应观察到玩家下落到接近 (6,0,2)");
        Assert.IsTrue(xStableDuringDescent, "下落期间 x 应保持为 6，实际发生了横向偏移");
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(
            new Vector3(6, 0, 2), player.transform.position,
            "玩家应停在下落一格后的位置");
        Assert.IsFalse(Game.instance.isMoving, "落地后不应继续下落");
        Object.DestroyImmediate(wallGo);
    }
}
