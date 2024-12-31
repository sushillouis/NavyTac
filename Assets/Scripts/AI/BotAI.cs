using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// public enum Activity
// {
//     None,
//     Forming,
//     Scout,
//     Persue,
//     Fighting,
//     Retreating
// }

[Serializable]
public class BotAI
{
    [Header("Paramaters")]
    // Max Ratio of enemy fleet to ai fleet to start an attack
    [SerializeField] float agressiveness = 1f;
    [SerializeField] float maxAttackRange = 5 * Utils.FromNauticalMiles;
    // Min Ratio of enemy fleet to ai fleet to start a retreat
    [SerializeField] float cowardice = 2f;
    [SerializeField] float engagementRadis = 1.5f * Utils.FromNauticalMiles;
    // Attack even beyond agressiveness
    [SerializeField] float desperation = 0f;
    [SerializeField] int maxSubGroupSize = 3;
    [SerializeField] int maxGroupStrength = 600;
    [SerializeField] int refreshrate = 10;
    float tickCounter = 0f;

    [SerializeField] List<Group> attackingGroups = new List<Group>();
    Group supplyGtroup = null;
    [SerializeField] List<Entity> entities = null;
    readonly TactPlayer self;
    public BotAI(TactPlayer player) {
        self = player;
    }

    public void Tick() {
        entities ??= GetAllTeamEntities();

        if(entities.Count==0) {
            return;
        }

        if(attackingGroups.Count == 0) {

            List<Entity> subGroup = new();
            List<Entity> attackGroup = new();
            int groupStrength = 0;
            bool containsCarrier = false;
            List<Entity> supportGroup = new();
            foreach (Entity ent in entities)
            {
                switch (ent.entityClass)
                {
                    case EntityClass.Sub:
                        subGroup.Add(ent);
                        if(subGroup.Count>maxSubGroupSize) {
                            attackingGroups.Add(TacticalAIMgr.inst.CreateGroup(subGroup, attackingGroups.Count));
                            subGroup.Clear();
                        }
                    break;
                    case EntityClass.Carrier:
                    case EntityClass.Cruiser:
                    case EntityClass.Destroyer:
                    case EntityClass.Frigate:
                    case EntityClass.LHA:
                    case EntityClass.USV:
                        if(ent.entityClass == EntityClass.Carrier) {
                            containsCarrier = true;
                        }
                        attackGroup.Add(ent);
                        groupStrength += Utils.strengthDict[ent.entityType];
                        if(groupStrength>maxGroupStrength && containsCarrier) {
                            Group temp = TacticalAIMgr.inst.CreateGroup(attackGroup, attackingGroups.Count);
                            temp.totalStrength = groupStrength;
                            temp.GetTotalCost();
                            attackingGroups.Add(temp);
                            attackGroup.Clear();
                            groupStrength=0;
                            containsCarrier = false;
                        }
                    break;
                    default:
                        supportGroup.Add(ent);
                    break;
                }
            }

            if(subGroup.Count>0) {
                attackingGroups.Add(TacticalAIMgr.inst.CreateGroup(subGroup, attackingGroups.Count));
            }

            if(attackGroup.Count>0) {
                Group temp = TacticalAIMgr.inst.CreateGroup(attackGroup, attackingGroups.Count);
                temp.totalStrength = groupStrength;
                attackingGroups.Add(temp);
            }

            if(supportGroup.Count>0) {
                supplyGtroup = TacticalAIMgr.inst.CreateGroup(supportGroup, -1);
            }
        }

        tickCounter+=Time.deltaTime;
        if(tickCounter<=refreshrate) {
            return;
        }
        tickCounter = 0;

        List<Entity> enemyEnts = GetAllEnemyEntities();
        bool[] attacking = new bool[enemyEnts.Count];
        enemyEnts.Sort();
        for (int i =0;i<attackingGroups.Count;i++) {
            
        }

    }

    List<Entity> GetAllTeamEntities() {
        List<Entity> temp = new();
        foreach(Entity ent in EntityMgr.inst.entities)  {
            if (ent != null && (ent.owner.playerId == self.playerId)) {

                temp.Add(ent);
            }
        }
        return temp;
    }

    List<Entity> GetAllEnemyEntities() {
        List<Entity> temp = new();
        foreach(Entity ent in EntityMgr.inst.entities)  {
            if (ent != null && (ent.owner.playerId != self.playerId)) {

                temp.Add(ent);
            }
        }
        return temp;
    }
}