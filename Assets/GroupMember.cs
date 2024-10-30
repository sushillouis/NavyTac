using UnityEngine;


[System.Serializable]
public class GroupMember : Command
{
    // Group myGroup;
    public EscortFormate(Entity ent, Entity target, Vector3 newOffset) {
        targetEntity = target;
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
