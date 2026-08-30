using System;

public enum State
{
    Menu,
    InClub,
    FirstPerson
}

public enum Trigger
{
    ToClub,
    ToFirstPerson,
    ToMainMenu
}

public class StateEventArgs : EventArgs
{
    public State target {get;}

    public StateEventArgs(State target)
    {
        this.target = target;
    }
}

public class StateMachine
{
    public State CurrentState { get; private set; } = State.Menu;

    public EventHandler<StateEventArgs> Enter;
    public EventHandler<StateEventArgs> Exit;

    public void Fire(Trigger trigger)
    {
        // Assumes user will never be able to go directly from Menu to the First Person poker game
        State nextState = (CurrentState, trigger) switch
        {
            (State.Menu, Trigger.ToClub)            => State.InClub,
            (State.InClub, Trigger.ToFirstPerson)   => State.FirstPerson,
            (State.InClub, Trigger.ToMainMenu)      => State.Menu,
            (State.FirstPerson, Trigger.ToClub)     => State.InClub,
            (State.FirstPerson, Trigger.ToMainMenu) => State.Menu,
            _ => throw new InvalidOperationException($"Invalid transition from {CurrentState} via {trigger}")
        };

        Exit?.Invoke(trigger, new StateEventArgs(CurrentState));
        CurrentState = nextState;
        Enter?.Invoke(trigger, new StateEventArgs(CurrentState));
    }

}
