using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public enum EntityType
{
    DDG51,
    Container,
    MineSweeper,
    OilServiceVessel,
    OrientExplorer,
    PilotVessel,
    SmitHouston,
    Tanker,
    TugBoat,
    JARIUSV,
    SeaHunter,
    Mykola,
    SeaBaby,
    CVN75,
    Submarine,
    AntiShipMissile,
}

[Serializable]
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

[Serializable]
public enum EntityClass
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
    Sub,
    Missile,
    Airplane,//more classes for airplanes easy to confuse with roles
}

[Serializable]
public enum EntityRole
{
    None = 0,
    Carrier,
    SurfaceAttack,
    SuicideAttack,
    AntiAir,
    ISR,
    ASW,
    NonCombat,
    AirSuperiority,
    GroundAttack,
    Bomber,
    CAP,
    SEAD,
}

[Serializable]
public enum TacticsType
{
    EscortMove = 0,
    Pincer,
    AtkMove,
    AtkDistract,
    Defend,
    Scout,
    Cancel,
    None,
}

[Serializable]
public enum MapNames
{
    None = 0,
    OpenOcean,
    FourCorners,
}

[Serializable]
public enum WeaponBehaviors
{
    None = 0,
    Dumb,
    SurfaceInterceptor,
    AirInterceptor,
    Smart,
}

[Serializable]
public enum TactCommandTypes
{
    Move = 0,
    Follow,
    Intercept,
    Intercept3d,
}
