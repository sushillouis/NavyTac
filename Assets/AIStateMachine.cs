using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base State Machine Framework
public class AIStateMachine : MonoBehaviour
{
    public AIState currentState;
    private Dictionary<System.Type, AIState> states = new Dictionary<System.Type, AIState>();

    void Update() => currentState?.OnUpdate();

    public void AddState(AIState state) => states[state.GetType()] = state;
    
    public void ChangeState<T>() where T : AIState
    {
        currentState?.OnExit();
        currentState = states[typeof(T)];
        currentState?.OnEnter();
    }
}

public abstract class AIState : MonoBehaviour
{
    protected AIStateMachine machine;
    // protected NavMeshAgent agent;
    // protected Unit unit;

    // void Awake()
    // {
    //     machine = GetComponent<AIStateMachine>();
    //     agent = GetComponent<NavMeshAgent>();
    //     unit = GetComponent<Unit>();
    // }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}
