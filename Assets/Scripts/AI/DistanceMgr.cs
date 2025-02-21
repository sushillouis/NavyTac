using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

// Keep all original serializable classes exactly the same
[Serializable]
public class SubPotential
{
    public Vector3 diff;
    public float distance;
    public Vector3 direction;
    public Transform pfTransform;
}

[Serializable]
public class Potential
{
    // Original Potential class implementation
    public Entity ownship;
    public Entity target;
    public float distance;
    public Vector3 diff;
    public Vector3 relativeVelocity;
    public Vector3 direction;
    public CPAInfo cpaInfo;
    public float targetAngle;
    public int framecount;
    public List<SubPotential> subPotentials;

    public Potential(Entity own, Entity tgt)
    {
        ownship = own;
        target = tgt;
        cpaInfo = new CPAInfo(own, target);
        subPotentials = new List<SubPotential>();
        foreach(Transform t in own.ai.pfList)
        {
            SubPotential subPotential = new SubPotential
            {
                distance = 0,
                diff = Vector3.zero,
                direction = Vector3.zero,
                pfTransform = t
            };
            subPotentials.Add(subPotential);
        }
    }

    public void ReCompute()
    {
        // This will now be called after GPU updates
        framecount = Time.frameCount;
        cpaInfo.ReCompute();
        foreach(SubPotential sp in subPotentials)
        {
            sp.diff = target.position - sp.pfTransform.position;
            sp.direction = sp.diff.normalized;
            sp.distance = sp.diff.magnitude;
        }
    }
}

[System.Serializable]
public class CPAInfo
{
    // Original CPAInfo implementation
    public Entity ownship;
    public Entity target;
    public Vector3 ownShipPosition = Vector3.zero;
    public Vector3 targetPosition = Vector3.zero;
    public float time = 0;
    public float range = 0;
    public float targetRelativeBearing = 0;
    public float targetAbsBearing = 0;
    public float targetAngle;
    public Vector3 relativeVelocity = Vector3.zero;

    Vector3 velDiff = Vector3.zero;
    Vector3 posDiff = Vector3.zero;
    float relSpeedSquared = 0;
    Vector3 diff;

    public CPAInfo(Entity e1, Entity e2)
    {
        ownship = e1;
        target = e2;
    }

    public void ReCompute()
    {
        // Maintain original CPU implementation
        velDiff = ownship.velocity - target.velocity;
        posDiff = ownship.position - target.position;
        relativeVelocity = target.velocity - ownship.velocity;
        relSpeedSquared = Vector3.Dot(velDiff, velDiff);
        if(relSpeedSquared < Utils.EPSILON * 10)
            time = 0;
        else
            time = -Vector3.Dot(posDiff, velDiff) / relSpeedSquared;
        if(time < 0) time = 0;
        ownShipPosition = ownship.position + ownship.velocity * time;
        targetPosition = target.position + target.velocity * time;
        range = Vector3.Distance(ownShipPosition, targetPosition);

        diff = targetPosition - ownShipPosition;
        targetAbsBearing = Utils.Degrees360(Utils.VectorToHeadingDegrees(diff));
        targetRelativeBearing = Utils.Degrees360(Utils.AngleDiffPosNeg(targetAbsBearing, ownship.heading));
        targetAngle = Utils.Degrees360(targetAbsBearing + 180 - target.heading);
    }
}

public class DistanceMgr : MonoBehaviour
{
    public static DistanceMgr inst;
    
    // Original fields
    public Potential[,] potentials2D;
    public Dictionary<Entity, Dictionary<Entity, Potential>> potentialsDictionary;
    public List<List<Potential>> potentialsList;
    public List<Potential> selectedEntityPotentials;
    public bool isInitialized = false;
    public int ii = 0;
    public int jj = 0;

    // GPU additions
    public ComputeShader computeShader;
    private ComputeBuffer entityBuffer;
    private ComputeBuffer potentialBuffer;
    
    private struct GPUEntity
    {
        public Vector3 position;
        public Vector3 velocity;
        public float heading;
    }

    private struct GPUPotential
    {
        public Vector3 diff;
        public float distance;
        public Vector3 direction;
        public Vector3 relativeVelocity;
        public float targetAngle;
    }

    void Awake()
    {
        inst = this;
    }

    public void Initialize()
    {
        // Original initialization logic
        isInitialized = true;
        potentialsDictionary = new Dictionary<Entity, Dictionary<Entity, Potential>>();
        potentialsList = new List<List<Potential>>();
        int n = EntityMgr.inst.entities.Count;
        potentials2D = new Potential[n, n];
        
        // Original ii/jj tracking
        ii = 0;
        foreach(Entity ent1 in EntityMgr.inst.entities)
        {
            Dictionary<Entity, Potential> ent1PotDictionary = new Dictionary<Entity, Potential>();
            List<Potential> ent1PotList = new List<Potential>();
            potentialsDictionary.Add(ent1, ent1PotDictionary);
            potentialsList.Add(ent1PotList);
            
            jj = 0;
            foreach(Entity ent2 in EntityMgr.inst.entities)
            {
                Potential pot = new Potential(ent1, ent2);
                ent1PotDictionary.Add(ent2, pot);
                ent1PotList.Add(pot);
                potentials2D[ii, jj] = pot;
                jj++;
            }
            ii++;
        }

        // GPU initialization
        InitializeGPU();
    }

    void InitializeGPU()
    {
        List<Entity> entities = EntityMgr.inst.entities;
        GPUEntity[] gpuEntities = new GPUEntity[entities.Count];

        for(int i = 0; i < entities.Count; i++)
        {
            Entity e = entities[i];
            gpuEntities[i] = new GPUEntity
            {
                position = e.position,
                velocity = e.velocity,
                heading = e.heading
            };
        }

        entityBuffer = new ComputeBuffer(entities.Count, Marshal.SizeOf(typeof(GPUEntity)));
        entityBuffer.SetData(gpuEntities);

        potentialBuffer = new ComputeBuffer(entities.Count * entities.Count, Marshal.SizeOf(typeof(GPUPotential)));
    }

    void Update()
    {
        if(isInitialized)
            UpdatePotentials();
        else
            Initialize();
    }

    void UpdatePotentials()
    {
        // GPU computation
        ComputeShaderCalculation();

        // Original selection logic
        for(int i = 0; i < EntityMgr.inst.entities.Count - 1; i++)
        {
            Entity ent1 = EntityMgr.inst.entities[i];
            if(ent1 == SelectionMgr.inst.selectedEntity)
                selectedEntityPotentials = potentialsList[i];
        }

        // CPU-side updates
        UpdateCPAData();
    }

    void ComputeShaderCalculation()
    {
        int kernel = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernel, "_Entities", entityBuffer);
        computeShader.SetBuffer(kernel, "_Potentials", potentialBuffer);
        computeShader.SetInt("_EntityCount", EntityMgr.inst.entities.Count);

        int threadGroups = Mathf.CeilToInt((float)(EntityMgr.inst.entities.Count * EntityMgr.inst.entities.Count) / 64);
        computeShader.Dispatch(kernel, threadGroups, 1, 1);

        // Retrieve data
        GPUPotential[] gpuResults = new GPUPotential[EntityMgr.inst.entities.Count * EntityMgr.inst.entities.Count];
        potentialBuffer.GetData(gpuResults);

        // Map back to original data structure
        int index = 0;
        for(int i = 0; i < EntityMgr.inst.entities.Count; i++)
        {
            for(int j = 0; j < EntityMgr.inst.entities.Count; j++)
            {
                Potential p = potentials2D[i, j];
                GPUPotential gp = gpuResults[index++];

                p.diff = gp.diff;
                p.distance = gp.distance;
                p.direction = gp.direction;
                p.relativeVelocity = gp.relativeVelocity;
                p.targetAngle = gp.targetAngle;
            }
        }
    }

    void UpdateCPAData()
    {
        // Original CPA and subpotential updates
        foreach(var row in potentialsDictionary.Values)
        {
            foreach(var potential in row.Values)
            {
                potential.ReCompute();
            }
        }
    }

    void OnDestroy()
    {
        entityBuffer?.Release();
        potentialBuffer?.Release();
    }

    // Original helper methods remain unchanged
    public Potential GetPotential(Entity e1, Entity e2)
    {
        return potentialsDictionary.ContainsKey(e1) && potentialsDictionary[e1].ContainsKey(e2) ? 
            potentialsDictionary[e1][e2] : 
            null;
    }
}