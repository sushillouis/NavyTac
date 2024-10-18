using UnityEngine;


[System.Serializable]
public class EscortFormate : Follow
{
    // Formation myFormation;
    public EscortFormate(Entity ent, Entity target, Vector3 newOffset): base(ent, target, Vector3.left)
    {
        targetEntity = target;
        relativeOffset = newOffset;
    }

    public void UpdateFormation(Vector3 newOffset) {
        relativeOffset = newOffset;
    }

    public override void Stop()
    {
        // myFormation.RemoveMember(entity.GetComponent<UnitAI>());
        base.Stop();
        entity.desiredSpeed = 0;
        isRunning = false;
    }
}
