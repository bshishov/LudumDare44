using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils.FSM
{
    /// <summary>
    /// Base component for running the state machine
    /// </summary>
    public class StateMachine<TStateKey>
        where TStateKey : struct
    {
        public IStateBehaviour<TStateKey> CurrentState => _currentState;
        public TStateKey? CurrentStateKey => _currentStateKey;
        public Action<TStateKey> StateChanged;

        private readonly Dictionary<TStateKey, IStateBehaviour<TStateKey>> _availableStates = new Dictionary<TStateKey, IStateBehaviour<TStateKey>>();
        private IStateBehaviour<TStateKey> _currentState;
        private TStateKey? _currentStateKey;

        public void AddState(TStateKey key, IStateBehaviour<TStateKey> state)
        {
            _availableStates.Add(key, state);
        }
        
        public void Update()
        {
            var nextStateKey = _currentState?.StateUpdate();
            if (nextStateKey.HasValue && !nextStateKey.Equals(_currentStateKey))
                SwitchToState(nextStateKey.Value);
        }

        public void SwitchToState(TStateKey nextStateKey)
        {
            _currentState?.StateEnded();

            if (_availableStates.TryGetValue(nextStateKey, out var nextState))
            {
                _currentState = nextState;
                _currentStateKey = nextStateKey;
                StateChanged?.Invoke(nextStateKey);
                _currentState.StateStarted();
            }
            else
            {
                _currentState = null;
                Debug.LogWarningFormat("No such state registered in state machine: {0}", nextStateKey);
            }
        }
    }
}
