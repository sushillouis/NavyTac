using System.Collections.Generic;
using UnityEngine;

public class GroupMove : Move
{
    public List<Entity> groupMembers;
    public FormationType formationType;
    public float formationSpacing = 300f;
    
    private Vector3 _destination;
    private Vector3 _formationRight;
    private Vector3 _formationForward;
    private int _entityIndex;
    private Vector3 _initialGroupCenter;

    public GroupMove(Entity ent, Vector3 pos, List<Entity> group, FormationType formation = FormationType.Circle) 
        : base(ent, pos)
    {
        _destination = pos;
        groupMembers = new List<Entity>(group);
        formationType = formation;
        _initialGroupCenter = CalculateInitialGroupCenter();
        SortGroupMembersByProximity();
        _formationForward = (_destination - _initialGroupCenter).normalized;
        _formationRight = Vector3.Cross(Vector3.up, _formationForward).normalized;
    }

    private Vector3 CalculateInitialGroupCenter()
    {
        Entity closest = null;
        float minDist = Mathf.Infinity;
        foreach (Entity member in groupMembers)
        {
            if (member != null)
            {
                float dist = Vector3.Distance(member.position, _destination);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = member;
                }
            }
        }
        return closest != null ? closest.position : Vector3.zero;
    }

    private void SortGroupMembersByProximity()
    {
        groupMembers.Sort((a, b) => 
            Vector3.Distance(a.position, _initialGroupCenter).CompareTo(
            Vector3.Distance(b.position, _initialGroupCenter)));
    }

    public override void Init()
    {
        _entityIndex = groupMembers.IndexOf(entity);
        CalculateFormationPosition();
        base.Init();
    }

    private void CalculateFormationPosition()
    {
        Entity leader = groupMembers[0];
        Vector3 leaderPosition = leader.position;
        _formationForward = (_destination - leaderPosition).normalized;
        _formationRight = Vector3.Cross(Vector3.up, _formationForward).normalized;

        switch(formationType)
        {
            case FormationType.Wedge:
                if (_entityIndex == 0)
                {
                    movePosition = _destination;
                }
                else
                {
                    int row = Mathf.FloorToInt((Mathf.Sqrt(1 + 8 * _entityIndex) - 1) / 2);
                    int indexInRow = _entityIndex - (row * (row + 1)) / 2;
                    float xOffset = (indexInRow - row * 0.5f) * formationSpacing;
                    float zOffset = row * formationSpacing;
                    movePosition = leaderPosition + 
                        (_formationForward * zOffset) + 
                        (_formationRight * xOffset);
                }
                break;

            case FormationType.Circle:
                float angle = 360f / groupMembers.Count * _entityIndex;
                float radius = formationSpacing * groupMembers.Count / (2 * Mathf.PI);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * _formationForward * radius;
                movePosition = leaderPosition + offset;
                break;

            case FormationType.Vee:
                if (_entityIndex == 0)
                {
                    movePosition = _destination;
                }
                else
                {
                    int adjustedIndex = _entityIndex - 1;
                    int row_Vee = adjustedIndex / 2;
                    bool isLeftSide = (adjustedIndex % 2) == 0;

                    float zOffset_Vee = row_Vee * formationSpacing;
                    float xOffset_Vee = (isLeftSide ? -1 : 1) * (row_Vee + 1) * formationSpacing;

                    movePosition = leaderPosition + 
                        (_formationForward * zOffset_Vee) + 
                        (_formationRight * xOffset_Vee);
                }
                break;

            case FormationType.InvertedVee:
                if (_entityIndex == 0)
                {
                    movePosition = _destination;
                }
                else
                {
                    int sideIndex = _entityIndex - 1;
                    int row_Ivee = (sideIndex / 2) + 1;
                    bool isLeft = (sideIndex % 2) == 0;
                    float xOffset_Ivee = (isLeft ? -1 : 1) * row_Ivee * formationSpacing;
                    float zOffset_Ivee = -row_Ivee * formationSpacing;
                    movePosition = leaderPosition + 
                        (_formationForward * zOffset_Ivee) + 
                        (_formationRight * xOffset_Ivee);
                }
                break;

            default: // Line formation
                float lineOffset = (_entityIndex - (groupMembers.Count - 1) / 2f) * formationSpacing;
                movePosition = leaderPosition + _formationRight * lineOffset;
                break;
        }
    }

    public override DHDS ComputePotentialDHDS(Vector3 movePositionParam)
    {
        // Check if any group member is within 500 units of the destination
        foreach (Entity member in groupMembers)
        {
            if (member != null && Vector3.Distance(member.position, _destination) <= 500f)
            {
                // Immediately stop all group members
                return new DHDS(entity.heading, 0f);
            }
        }

        // Recalculate formation position based on current leader's position
        CalculateFormationPosition();

        // Calculate potentials
        repulsivePotential = Vector3.zero;
        foreach(Entity ent in EntityMgr.inst.entities)
        {
            if(ent == entity || ent == null) continue;

            bool isGroupMember = groupMembers.Contains(ent);
            float repulsionStrength = isGroupMember ? 
                AIMgr.inst.groupRepulsiveCoefficient : 
                AIMgr.inst.repulsiveCoefficient;

            Potential p = DistanceMgr.inst.GetPotential(entity, ent);
            if(p.distance < AIMgr.inst.potentialDistanceThreshold)
            {
                repulsivePotential += p.direction * repulsionStrength *
                    Mathf.Pow(p.diff.magnitude, AIMgr.inst.repulsiveExponent);
            }
        }

        // Use the updated movePosition field instead of the parameter
        diffToMovePosition = this.movePosition - entity.position;
        attractivePotential = diffToMovePosition.normalized * 
                            AIMgr.inst.attractionCoefficient *
                            Mathf.Pow(diffToMovePosition.magnitude, 
                                    AIMgr.inst.attractiveExponent);

        potentialSum = attractivePotential - repulsivePotential;
        
        dh = Utils.Degrees360(Mathf.Rad2Deg * Mathf.Atan2(potentialSum.x, potentialSum.z));
        angleDiff = Utils.Degrees360(Utils.AngleDiffPosNeg(dh, entity.heading));
        cosValue = (Mathf.Cos(angleDiff * Mathf.Deg2Rad) + 1) / 2f;
        ds = entity.cruiseSpeed * cosValue;

        return new DHDS(dh, ds);
    }

    public override void Stop()
    {
        groupMembers.Clear();
        base.Stop();
    }
}