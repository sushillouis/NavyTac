using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PlayerID
{
    None = 0,
    PlayerOne = 1,
    PlayerTwo = 2,
    PlayerThree = 3,
    PlayerFour = 4,
    PlayerFive = 5,
    PlayerSix = 6,
    PlayerSeven = 7,
    PlayerEight = 8,
    Observer = 9,
    Admin = 10,
}

public enum PlayerSide
{
    None = 0,
    SideOne = 1,
    SideTwo = 2,
    SideThree = 3,
    SideFour = 4,
    SideFive = 5,
    SideSix = 6,
    SideSeven = 7,
    SideEight = 8,
    Observer = 9,
    Admin = 10,
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

