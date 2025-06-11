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
    Rig_Balder,
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
    Base
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

[Serializable]
public enum AIState
{
    None = 0,
    Move,
    Attack,
    Flee,
    Scout,
    Patrol,
    Chase,
    Dead,
    Orbit,
    GroupMove,
}

[Serializable]

public enum FormationType
{
    Line = 0,
    Wedge,
    Circle,
    Vee,
    InvertedVee
}

public enum Difficulty { Easy, Medium, Hard }

public enum LobbyState
{
    None = 0,
    SingleMultiPlayer,
    Login,
    MapSelect,
    HostOrJoin,
    Play,
    Done,
    ScorePanel,
    GamePaused,
    Replay,
    MultiScorePanel,
    }
public enum TrainingState
{
    None,
    PreTest,
    PostTest,
    Adaptive,
    NonAdaptive,
    Tutorial
    }

    public enum PathfindingState
{
    RequestingPath,
    FollowingPath,
    PotentialFieldsOnly,
    Finished
}