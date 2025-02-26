// // AIAgent.cs
// using UnityEngine;

// public class AIAgent {
//     public Entity entity;
//     public AIStateMachine fsm;
//     public AIState currentState;
//     public Vector3 destination;
//     public Vector3 moveDirection;
//     public ScoutParameters scoutParams = new ScoutParameters();
//     public OrbitParameters orbitParams = new OrbitParameters();
//     public Entity detectedTarget;

//     public AIAgent(Entity entity) {
//         this.entity = entity;
//         this.fsm = new AIStateMachine(this);
//         InitializeState();
//     }

//     private void InitializeState() {
//         fsm.Initialize(EnemyAIMgr.Instance.currentLevel == 1 ? 
//             (IAIState)new MoveState() : 
//             new AttackState());
//     }

//     public void Update() {
//         UpdateDetection();
//         fsm.Update();
//     }

//     private void UpdateDetection() {
//         detectedTarget = FindNearestEntity();
//     }

//     private Entity FindNearestEntity() {
//         Entity nearest = null;
//         float closestDistance = Mathf.Infinity;
        
//         foreach (Entity e in EntityMgr.inst.entities) {
//             if (IsValidTarget(e)) {
//                 float distance = Vector3.Distance(entity.position, e.position);
//                 if (distance < scoutParams.detectionRadius && distance < closestDistance) {
//                     nearest = e;
//                     closestDistance = distance;
//                 }
//             }
//         }
//         return nearest;
//     }

//     private bool IsValidTarget(Entity e) {
//         return e != entity && 
//                e.owner.name != "Ai" && 
//                !WeaponsMgr.inst.weapons.Contains(e) && 
//                e.creatorsEntity == null;
//     }
// }