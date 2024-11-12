using System.Collections.Generic;

public class DamageMatrix
{
    private Dictionary<WeaponBehaviors, Dictionary<EntityType, float>> damageMatrix;

    // Constructor to initialize the damage matrix
    public DamageMatrix()
    {
        InitializeDamageMatrix();
    }

    // Method to initialize the damage matrix
    private void InitializeDamageMatrix()
    {
        damageMatrix = new Dictionary<WeaponBehaviors, Dictionary<EntityType, float>>();

        // Define damage for each weaponBehaviours-target combination
        damageMatrix[WeaponBehaviors.Smart] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 5f },
            {EntityType.Container, 1f },
            {EntityType.CVN75, 2f },
            {EntityType.JARIUSV, 1f },
            {EntityType.MineSweeper, 2f },
            {EntityType.OilServiceVessel, 0 },
            {EntityType.OrientExplorer, 0 },
            {EntityType.PilotVessel, 0 },
            {EntityType.SeaBaby, 0 },
            {EntityType.SeaHunter, 0 },
            {EntityType.SmitHouston, 0 },
            {EntityType.Tanker, 0 },
            {EntityType.TugBoat, 0 },
            {EntityType.Mykola, 0 }
        };

        damageMatrix[WeaponBehaviors.AirInterceptor] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 100f },
            {EntityType.Container, 99f },
            {EntityType.CVN75, 2f },
            {EntityType.JARIUSV, 1f },
            {EntityType.MineSweeper, 2f },
            {EntityType.OilServiceVessel, 0 },
            {EntityType.OrientExplorer, 0 },
            {EntityType.PilotVessel, 0 },
            {EntityType.SeaBaby, 0 },
            {EntityType.SeaHunter, 0 },
            {EntityType.SmitHouston, 0 },
            {EntityType.Tanker, 0 },
            {EntityType.TugBoat, 0 },
            {EntityType.Mykola, 0 }
        };

        damageMatrix[WeaponBehaviors.SurfaceInterceptor] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 5f },
            {EntityType.Container, 1f },
            {EntityType.CVN75, 2f },
            {EntityType.JARIUSV, 1f },
            {EntityType.MineSweeper, 2f },
            {EntityType.OilServiceVessel, 0 },
            {EntityType.OrientExplorer, 0 },
            {EntityType.PilotVessel, 0 },
            {EntityType.SeaBaby, 0 },
            {EntityType.SeaHunter, 0 },
            {EntityType.SmitHouston, 0 },
            {EntityType.Tanker, 0 },
            {EntityType.TugBoat, 0 },
            {EntityType.Mykola, 0 }
        };

        damageMatrix[WeaponBehaviors.Dumb] = new Dictionary<EntityType, float>
        {
            {EntityType.DDG51, 5f },
            {EntityType.Container, 1f },
            {EntityType.CVN75, 2f },
            {EntityType.JARIUSV, 1f },
            {EntityType.MineSweeper, 2f },
            {EntityType.OilServiceVessel, 0 },
            {EntityType.OrientExplorer, 0 },
            {EntityType.PilotVessel, 0 },
            {EntityType.SeaBaby, 0 },
            {EntityType.SeaHunter, 0 },
            {EntityType.SmitHouston, 0 },
            {EntityType.Tanker, 0 },
            {EntityType.TugBoat, 0 },
            {EntityType.Mykola, 0 }
        };
    }

    // Method to get damage value for a given weapon and entity type
    public float GetDamage(WeaponBehaviors wb, EntityType entityType)
    {
        if (damageMatrix.ContainsKey(wb) && damageMatrix[wb].ContainsKey(entityType))
        {
            return damageMatrix[wb][entityType];
        }
        return 0f; // Default damage value if not found
    }
}