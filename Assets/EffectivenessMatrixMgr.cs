using System.Collections.Generic;

public static class EffectivenessMatrixMgr
{
    // Effectiveness matrix: WeaponType -> EntityType -> Effectiveness Value (0.0 to 1.0)
    private static Dictionary<WeaponType, Dictionary<EntityType, float>> effectivenessMatrix = new Dictionary<WeaponType, Dictionary<EntityType, float>>()
    {
        {
            WeaponType.Missile, new Dictionary<EntityType, float>()
            {
                { EntityType.Container, 1.0f },
                { EntityType.DDG51, 0.5f },
                { EntityType.MineSweeper, 0.8f },
                { EntityType.OilServiceVessel, 0.3f },
                { EntityType.OrientExplorer, 0.7f },
                { EntityType.PilotVessel, 0.8f },
                { EntityType.SmitHouston, 0.2f },
                { EntityType.Tanker, 0.1f },
                { EntityType.TugBoat, 0.2f }
            }
        }
        
    };

    
    public static float GetWeaponEffectiveness(WeaponType weaponType, EntityType targetType)
    {
        if (effectivenessMatrix.ContainsKey(weaponType) && effectivenessMatrix[weaponType].ContainsKey(targetType))
        {
            return effectivenessMatrix[weaponType][targetType];
        }
        return 0.0f; 
    }
}
