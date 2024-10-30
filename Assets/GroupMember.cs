using UnityEngine;


[System.Serializable]
public class GroupMember : Command
{
    // Group myGroup;
    public GroupMember(Entity ent, Entity target, Vector3 newOffset) : base(ent){
        // targetEntity = target;
        // relativeOffset = newOffset;
    }

    public override void Stop()
    {
        // myGroup.RemoveMember(entity.GetComponentInChildren<UnitAI>());
        base.Stop();
        entity.desiredSpeed = 0;
        isRunning = false;
    }
}
