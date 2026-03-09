using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Assets.Scripts
{
    //磁铁面的类型
    public enum PolarType
    {
        None,
        S,
        N,
        XPS,
        XPN,
        YPS,
        YPN,
        ZPS,
        ZPN,
    }
    //磁铁S面的朝向,P正,N负
    public enum MagnetType
    {
        None,
        XP,
        XN,
        YP,
        YN,
        ZP,
        ZN,
    }
    //磁力分类
    public enum Force
    {
        None,
        Attract,
        Repel,
    }
    //移动的方向
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Forward,
        Back,
    }

}