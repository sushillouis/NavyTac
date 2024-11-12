using System.Collections.Generic;

public class DamageMatrix
{
    private static Dictionary<EntityType, Dictionary<EntityType, float>> damageMatrix = new Dictionary<EntityType, Dictionary<EntityType, float>>()
    {
        {
            EntityType.Mykola, new Dictionary<EntityType, float>()
            {
                { EntityType.DDG51, 5f },
                { EntityType.Container, 1f },
                { EntityType.CVN75, 2f },
                { EntityType.JARIUSV, 1f },
                { EntityType.MineSweeper, 2f },
                { EntityType.OilServiceVessel, 0f },
                { EntityType.OrientExplorer, 0f },
                { EntityType.PilotVessel, 0f },
                { EntityType.SeaBaby, 0f },
                { EntityType.SeaHunter, 0f },
                { EntityType.SmitHouston, 0f },
                { EntityType.Tanker, 0f },
                { EntityType.TugBoat, 0f },
                { EntityType.Mykola, 0f }
            }
        },
        {
            EntityType.SeaBaby, new Dictionary<EntityType, float>()
            {
                { EntityType.DDG51, 100f },
                { EntityType.Container, 99f },
                { EntityType.CVN75, 2f },
                { EntityType.JARIUSV, 1f },
                { EntityType.MineSweeper, 2f },
                { EntityType.OilServiceVessel, 0f },
                { EntityType.OrientExplorer, 0f },
                { EntityType.PilotVessel, 0f },
                { EntityType.SeaBaby, 0f },
                { EntityType.SeaHunter, 0f },
                { EntityType.SmitHouston, 0f },
                { EntityType.Tanker, 0f },
                { EntityType.TugBoat, 0f },
                { EntityType.Mykola, 0f }
            }
        },
        {
            EntityType.AntiShipMissile, new Dictionary<EntityType, float>()
            {
                { EntityType.DDG51, 5f },
                { EntityType.Container, 1f },
                { EntityType.CVN75, 2f },
                { EntityType.JARIUSV, 1f },
                { EntityType.MineSweeper, 2f },
                { EntityType.OilServiceVessel, 0f },
                { EntityType.OrientExplorer, 0f },
                { EntityType.PilotVessel, 0f },
                { EntityType.SeaBaby, 0f },
                { EntityType.SeaHunter, 0f },
                { EntityType.SmitHouston, 0f },
                { EntityType.Tanker, 0f },
                { EntityType.TugBoat, 0f },
                { EntityType.Mykola, 0f }
            }
        },
        {
            EntityType.SeaHunter, new Dictionary<EntityType, float>()
            {
                { EntityType.DDG51, 5f },
                { EntityType.Container, 1f },
                { EntityType.CVN75, 2f },
                { EntityType.JARIUSV, 1f },
                { EntityType.MineSweeper, 2f },
                { EntityType.OilServiceVessel, 0f },
                { EntityType.OrientExplorer, 0f },
                { EntityType.PilotVessel, 0f },
                { EntityType.SeaBaby, 0f },
                { EntityType.SeaHunter, 0f },
                { EntityType.SmitHouston, 0f },
                { EntityType.Tanker, 0f },
                { EntityType.TugBoat, 0f },
                { EntityType.Mykola, 0f }
            }
        }
    };

    // Method to get the damage value
    public static float GetDamage(EntityType attacker, EntityType target)
    {
        if (damageMatrix.ContainsKey(attacker) && damageMatrix[attacker].ContainsKey(target))
        {
            return damageMatrix[attacker][target];
        }
        return 0f;
    }
}
