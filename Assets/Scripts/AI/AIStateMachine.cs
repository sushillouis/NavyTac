// // StateMachine.cs
// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
// using System;
// public class AIStateMachine
// {
//     private IAIState currentState;
//     private AIAgent agent;

//     public AIStateMachine(AIAgent agent)
//     {
//         this.agent = agent;
//     }

//     public void Initialize(IAIState initialState)
//     {
//         currentState = initialState;
//         currentState.Enter(agent);
//     }

//     public void TransitionTo(IAIState newState)
//     {
//         currentState?.Exit(agent);
//         currentState = newState;
//         currentState.Enter(agent);
//     }

//     public void Update()
//     {
//         currentState?.Update(agent);
//     }

//     internal void TransitionTo(MoveState moveState)
//     {
//         throw new NotImplementedException();
//     }
// }