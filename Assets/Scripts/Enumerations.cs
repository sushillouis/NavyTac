using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public enum PlayerSide
{
    SideOne = 0,
    SideTwo, 
    SideThree,
    SideFour,
    SideFive,
    SideSix,
    SideSeven,
    SideEight,
}

public enum ShipClasses
{
    None = 0,
    Carrier,
    Cruiser,
    Destroyer,
    Frigate,
    USV,
    MineSweeper,
    Pilot,
    Merchant,
    Tug,
    LHA,
    Supply,
}

public enum ShipRoles
{
    None = 0,
    Carrier,
    SurfaceAttack,
    SuicideAttack,
    AntiAir,
    ISR,
    ASW,
    NonCombat,
}

public enum TacticsType
{
    None = 0,
    Pincer,
    Formate,
    FormationAttack,
    AttackDistract,
    Scout,
}

public enum MapNames
{
    None = 0,
    OpenOcean,
    FourCorners,
}

public enum WeaponBehaviors
{
    None = 0,
    Dumb,
    SurfaceInterceptor,
    AirInterceptor,
    Smart,
}

public enum TactCommandTypes
{
    Move = 0,
    Follow,
    Intercept,
    Intercept3d,
}
