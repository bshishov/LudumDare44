using System;
using UnityEngine;

namespace Utils.FSM
{
    public abstract class BaseStateBehaviour<TStateKey> : IStateBehaviour<TStateKey>
        where TStateKey : struct
    {
        protected GameObject gameObject;
        protected Transform transform;

        protected BaseStateBehaviour(GameObject gameObject)
        {
            this.gameObject = gameObject;
            this.transform = gameObject.transform;
        }

        public abstract void StateStarted();

        public abstract TStateKey? StateUpdate();

        public abstract void StateEnded();

        public TComponent GetComponent<TComponent>()
        {
            return this.gameObject.GetComponent<TComponent>();
        }
    }
}