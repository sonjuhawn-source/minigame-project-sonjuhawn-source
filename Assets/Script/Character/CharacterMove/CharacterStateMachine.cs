using System;
using UnityEngine;

public class CharacterStateMachine : MonoBehaviour
{
    public enum PlayerState
    {
        Idle, 
        Moving, 
        Attacking, 
        HeavyAttacking, 
        Dodging, 
        Damaged, 
        Dead
    }

    public PlayerState CurrentState {  get; private set; }
    public bool IsInvincible { get; set; }

    public event Action<PlayerState> OnStateChanged;

    void Start()
    {
        ChangeState(PlayerState.Idle);
    }

    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;  
        CurrentState = newState;

        if (newState == PlayerState.Attacking ||
            newState == PlayerState.HeavyAttacking)
        {
            GetComponent<CharacterMover>().EnterAttack();
        }

        OnStateChanged?.Invoke(newState);
    }

    public bool IsState(PlayerState state)
    {
        return CurrentState == state;
    }
}
