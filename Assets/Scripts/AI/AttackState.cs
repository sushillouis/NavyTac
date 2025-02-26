// // AttackState.cs
// using UnityEngine;
// using System.Collections.Generic;
// public class AttackState : IAIState
// {
//     public void Enter(AIAgent agent)
//     {
//         agent.currentState = AIState.Attack;
//         EnemyAIMgr.Instance.RegisterAttacker(agent);
//         agent.entity.desiredSpeed = agent.entity.maxSpeed;
//     }

//     public void Update(AIAgent agent)
//     {
//         if (ShouldDisengage(agent))
//         {
//             agent.fsm.TransitionTo(new MoveState());
//             return;
//         }

//         // MaintainFormation(agent);
//         ExecuteAttack(agent);
//     }

//     public void Exit(AIAgent agent)
//     {
//         EnemyAIMgr.Instance.UnregisterAttacker(agent);
//     }

//     private bool ShouldDisengage(AIAgent agent)
//     {
//         return agent.detectedTarget == null ||
//                Vector3.Distance(agent.entity.position, agent.detectedTarget.position) >
//                agent.scoutParams.detectionRadius;
//     }

//     private void ExecuteAttack(AIAgent agent)
//     {
//         switch (EnemyAIMgr.Instance.attackLevel)
//         {
//             // case AiAttackLevel.Low:
//             //     WeaponsMgr.inst.FireWeapon(agent.entity, agent.detectedTarget);
//             //     break;
//         }
//     }
// }