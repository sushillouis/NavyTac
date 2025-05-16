using UnityEngine;
public class WeaponCollisionHandler : MonoBehaviour
{
    public Entity entity;
    private WeaponData weaponData;
    void Start()
    {
        entity = GetComponent<Entity>();
    }

    void OnCollisionEnter(Collision collision) {
    if (WeaponsMgr.inst.weapons.Contains(entity)) {
        Entity otherEntity = collision.collider.GetComponent<Entity>();
        
        if (collision.collider is TerrainCollider) {
            FXMgr.inst.CreateExplosionAt(collision.contacts[0].point, 1);
            WeaponsMgr.inst.DestroyEntity(entity);
            return;
        }

        if (otherEntity != null && otherEntity != entity.creatorsEntity && 
            otherEntity.owner != entity.owner && otherEntity.entityType != entity.entityType) {
            
            float damage = WeaponsMgr.inst.damageMatrix.GetDamage(entity.entityType, otherEntity.entityType);
            
            // Track damage before applying
            bool isPlayerAttacker = entity.owner != null && 
                !entity.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase);
            bool isPlayerTarget = otherEntity.owner != null && 
                !otherEntity.owner.name.Equals("Ai", System.StringComparison.OrdinalIgnoreCase);
                if (ScoreMgr.inst == null)
                {
                    //Debug.LogWarning("ScoreMgr instance is null, cannot track damage.");
                }
                else
                {
                    if (isPlayerAttacker)
                {
                    ScoreMgr.inst.damageDealt += damage;
                }
                    if (isPlayerTarget) {
                    ScoreMgr.inst.damageTaken += damage;
                }
                }
           

            FXMgr.inst.CreateExplosionAt(collision.contacts[0].point, 1);
            otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
            WeaponsMgr.inst.DestroyEntity(entity);
        }
    }
}
}
