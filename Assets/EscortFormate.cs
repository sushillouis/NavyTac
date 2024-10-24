using UnityEngine;


[System.Serializable]
public class EscortFormate : Follow
{
    // Group myGroup;
    public EscortFormate(Entity ent, Entity target, Vector3 newOffset): base(ent, target, Vector3.left)
    {
        targetEntity = target;
        relativeOffset = newOffset;
    }

    public void UpdateGroup(Vector3 newOffset) {
        relativeOffset = newOffset;
    }

    public override void Stop()
    {
        // myGroup.RemoveMember(entity.GetComponentInChildren<UnitAI>());
        base.Stop();
        entity.desiredSpeed = 0;
        isRunning = false;
    }
}
