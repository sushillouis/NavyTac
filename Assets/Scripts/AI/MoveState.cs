// // MoveState.cs
// using UnityEngine;
// public class MoveState : IAIState
// {
//     public void Enter(AIAgent agent)
//     {
//         agent.currentState = IAiState.Move;
//         agent.moveDirection = agent.entity.transform.forward;
//         agent.destination = CalculateInitialDestination(agent);
//         IssueMoveCommand(agent);
//     }

//     public void Update(AIAgent agent)
//     {
//         if (DestinationReached(agent))
//         {
//             agent.destination = CalculateNextDestination(agent);
//             IssueMoveCommand(agent);
//         }

//         if (agent.detectedTarget != null)
//         {
//             agent.fsm.TransitionTo(new AttackState());
//         }
//     }

//     public void Exit(AIAgent agent) { }

//     private Vector3 CalculateInitialDestination(AIAgent agent)
//     {
//         return agent.entity.position + agent.moveDirection.normalized * 500f;
//     }

//     private Vector3 CalculateNextDestination(AIAgent agent)
//     {
//         Vector3 desired = agent.entity.position + agent.moveDirection.normalized * 500f;
//         return ApplyBoundaryConstraints(desired, agent);
//     }

//     private Vector3 ApplyBoundaryConstraints(Vector3 position, AIAgent agent)
//     {
//         return new Vector3(
//             Mathf.Clamp(position.x, -9000f, 9000f),
//             position.y,
//             Mathf.Clamp(position.z, -9000f, 9000f)
//         );
//         // Boundary constraint implementation
//     }
// }