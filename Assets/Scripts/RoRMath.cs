using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoRMath 
{

    public const float EPSILON = 0.001f;
    public const float EPSILON_DEGREES = 0.1f;
    public const float ONE_POINT_DEGREES = 11.25f;

    /// <summary>
    /// Returns true if x is with <c>EPSILON</c> of y
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool ApproximatelyEqual(float x, float y)
    {
        return (Mathf.Abs(x - y) <= EPSILON);
    }
    public static bool ApproximatelyEqual(float x, float y, float slack)
    {
        return (Mathf.Abs(x - y) <= slack);
    }

    /// <summary>
    /// returns true if x is with <c> EPSILON_DEGREES </c> of y
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool DegreesApproximatelyEqual(float x, float y)
    {
        return (Mathf.Abs(x - y) <= EPSILON_DEGREES);
    }

    public static float MakeAnglePosDeg(float angle)
    {
        while(angle < 0)
            angle += 360.0f;
        while(angle > 360.0f)
            angle -= 360.0f;
        return angle;
    }
    public static float makeAnglePosNegRad(float angle)
    {
        while(angle > Mathf.PI)
            angle -= Mathf.PI * 2.0f;
        while(angle < -Mathf.PI)
            angle += Mathf.PI * 2.0f;
        return angle;
    }
    public static float makeAnglePosNegDeg(float angle)
    {
        while(angle > 180.0f)
            angle -= 360.0f;
        while(angle < -180.0f)
            angle += 360.0f;
        return angle;
    }
    public static float DifferenceBetweenAnglesDeg(float angle1, float angle2)//must be 0-360
    {
        return makeAnglePosNegDeg(angle1 - angle2);
    }
    public static float DifferenceBetweenAnglesRad(float angle1, float angle2)
    {
        return makeAnglePosNegRad(angle1 - angle2);
    }
    public static float FromTargetAngleToHeading(float targetAngleDegrees)
    {
        float arrowAngle = makeAnglePosNegDeg(180 - targetAngleDegrees);
        return arrowAngle;
    }

    public static Vector3 VectorFromAngle(float angleDegrees)
    {
        Vector3 vec = new Vector3(Mathf.Sin(angleDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleDegrees * Mathf.Deg2Rad));
        return vec;
    }

    internal static float FromKnots(float knots)
    {
        return knots * 0.514444444f;
    }
    internal static float ToKnots(float speed)
    {
        return speed / FromKnots(1.0f);
    }

    internal static float FromNauticalMiles(float distanceNM)
    {
        return distanceNM * 1852;
    }

    internal static float ToNauticalMiles(float dist)
    {
        return dist * 0.000539957f;
    }

    internal static float ToYards(float dist)
    {
        return dist * 1.09361f;
    }

    internal static float FromDegrees(float degrees)
    {
        return UnityEngine.Mathf.Deg2Rad * degrees;
    }

    internal static float VectorToHeading(Vector3 v)
    {
        return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
    }
    internal static float RelativeBearingDegrees(Entity ownship, Entity track)
    {
        return RelativeBearingDegreesPos(ownship, track.position);
        /*
        Vector3 diff = track.phx.position - ownship.phx.position;
        float bearing = RoRMath.MakeAnglePosDeg(RoRMath.VectorToHeading(diff));
        float relativeBearing = RoRMath.MakeAnglePosDeg(RoRMath.DifferenceBetweenAnglesDeg(bearing, ownship.phx.headingInDegrees));
        return relativeBearing;
        */
    }

    internal static float RelativeBearingDegreesPos(Entity ownship, Vector3 targetPos)
    {
        return RelativeBearingDegreesPosHeading(ownship.position, ownship.heading, targetPos);
    }

    internal static float RelativeBearingDegreesPosHeading(Vector3 ownPos, float ownHeadingDegrees, Vector3 targetPos)
    {
        Vector3 diff = targetPos - ownPos;
        float bearing = MakeAnglePosDeg(VectorToHeading(diff));
        float relativeBearing = MakeAnglePosDeg(DifferenceBetweenAnglesDeg(bearing, ownHeadingDegrees));
        //float relativeBearing = MakeAnglePosDeg(DifferenceBetweenAnglesDeg(ownHeadingDegrees, bearing));
        return relativeBearing;
    }

    internal static CPAInfo CPA(Entity ent1, Entity ent2)
    {


        CPAInfo cpa = new CPAInfo(ent1, ent2);

        Vector3 velDiff = ent1.velocity - ent2.velocity;
        Vector3 posDiff = ent1.position - ent2.position;
        cpa.relativeVelocity = ent2.velocity - ent2.velocity;

        float relSpeedSquared = Vector3.Dot(velDiff, velDiff);
        if(relSpeedSquared < EPSILON * 10)
            cpa.time = 0;
        else 
            cpa.time = - Vector3.Dot(posDiff, velDiff)/relSpeedSquared;

        if(cpa.time < 0) cpa.time = 0;

        cpa.ownShipPosition = ent1.position + (ent1.velocity * cpa.time);
        cpa.targetPosition = ent2.position + (ent2.velocity * cpa.time);
        cpa.range = Vector3.Distance(cpa.ownShipPosition, cpa.targetPosition);

        Vector3 diff = cpa.targetPosition - cpa.ownShipPosition;
        float absBearing = RoRMath.MakeAnglePosDeg(RoRMath.VectorToHeading(diff));
        cpa.targetRelativeBearing = RoRMath.MakeAnglePosDeg(RoRMath.DifferenceBetweenAnglesDeg(absBearing, ent1.heading));

        return cpa;
    }

    internal static float TargetAngle(Entity ownship, Entity target)
    {
        float targetHeading = RoRMath.MakeAnglePosDeg(target.heading);
        Vector3 diffVec = target.position - ownship.position;
        float absoluteBearing = RoRMath.MakeAnglePosDeg(Mathf.Atan2(diffVec.x, diffVec.z) * Mathf.Rad2Deg);
        float targetAngle = RoRMath.MakeAnglePosDeg(absoluteBearing + 180 - targetHeading);
        return targetAngle;
    }
}
