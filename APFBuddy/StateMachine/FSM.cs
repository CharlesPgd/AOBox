using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Stateless;

namespace APFBuddy
{
    public enum Trigger
    {
        StartNewRun,
        TeamReformFailed,
        SectorEntered,
        APFHubEntered,
        UnknownPlayfieldEntered,
        Halt,
        PullTargetSighted,
        BadTarget,
        PullTimedOut,
        MobEnteredCombatRange,
        NoMobsInRange,
        ReachedEndOfSector
    }

    public enum State
    {
        None,
        Idle,
        EnterSector,
        Patrol,
        Pull,
        Fight,
        Reform,
        Leech
    }

    public class FSM<TState, TTrigger> : StateMachine<TState, TTrigger>
    {
        private IState _executingState = null;
        private Dictionary<TState, Type> _states = new Dictionary<TState, Type>();
        private bool _initialized = false;

        public FSM(TState defaultState) : base(defaultState)
        {
        }

        public void Tick()
        {
            if (!_initialized)
            {
                EnterState(State);
                _initialized = true;
            }

            _executingState.Tick();
        }

        private void EnterState(TState state)
        {
            _executingState = (IState)Activator.CreateInstance(_states[state]);
            _executingState.OnStateEnter();
        }

        private void ExitState()
        {
            _executingState.OnStateExit();
        }

        public void SetDefaultState(TState state)
        {
            EnterState(state);
        }

        public StateConfiguration AddState(TState state, Type stateType)
        {
            if (!_states.ContainsKey(state))
                _states.Add(state, stateType);

            return Configure(state).OnEntry(() => EnterState(state)).OnExit(() => ExitState());
        }

        public void AddGlobalPermit(TTrigger trigger, TState destinationState)
        {
            foreach(TState state in Enum.GetValues(typeof(TState)))
            {
                if(state.Equals(destinationState))
                    Configure(state).Ignore(trigger);
                else
                    Configure(state).Permit(trigger, destinationState);
            }
        }
    }
}
