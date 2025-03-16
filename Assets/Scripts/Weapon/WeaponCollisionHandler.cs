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
            if (collision.collider is TerrainCollider) {
                Debug.Log(collision.collider.name);
                FXMgr.inst.CreateExplosionAt(collision.contacts[0].point, 1);
                WeaponsMgr.inst.DestroyEntity(entity);
                return;
            }

            Entity otherEntity = collision.collider.GetComponent<Entity>();
            if (otherEntity != null && otherEntity != entity.creatorsEntity && otherEntity.owner != entity.owner && otherEntity.entityType != entity.entityType) {
                float damage = WeaponsMgr.inst.damageMatrix.GetDamage(entity.entityType, otherEntity.entityType);
                Debug.Log(otherEntity.name+" "+ otherEntity.entityType+" "+ otherEntity.owner );
                FXMgr.inst.CreateExplosionAt(collision.contacts[0].point, 1);
                otherEntity.health = Mathf.Max(otherEntity.health - damage, 0);
                WeaponsMgr.inst.DestroyEntity(entity);
            }
        }
    }
}
