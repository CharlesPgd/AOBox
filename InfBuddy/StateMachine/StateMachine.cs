namespace InfBuddy
{
    public class StateMachine
    {
        public IState CurrentState { get; private set; } = null;

        public StateMachine(IState defaultState)
        {
            SetState(defaultState);
        }

        public void Tick()
        {
            IState nextState = CurrentState.GetNextState();

            if(nextState != null)
                SetState(nextState);

            CurrentState.Tick();
        }
        public void SetState(IState state, bool triggerEvents = true)
        {
            if(CurrentState != null && triggerEvents)
                CurrentState.OnStateExit();

            CurrentState = state;

            if(triggerEvents)
                state.OnStateEnter();
        }
    }
}
