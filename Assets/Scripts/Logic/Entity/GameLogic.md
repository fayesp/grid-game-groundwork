# Game.cs

## 方法解释

### MoveStart()方法:

> 将Moves的位置记录到PlannedMoves数组,默认常规移动只有一轮MoveCycle,后面几轮都是下落,PlannedMoves.Add(GetMoversPositions());记录当前的位置,执行mover.ExecuteLogicalMove()是移动之后的位置,如果有下落就会循环执行记录,最后的位置在循环之外执行PlannedMoves.Add(GetMoversPositions())记录.
>
> 常规的推箱子没有连动的设置,第一轮移动之后,后续的移动都默认是下落"StartMoveCycle(true); // 开始下落动画"
>
> 

