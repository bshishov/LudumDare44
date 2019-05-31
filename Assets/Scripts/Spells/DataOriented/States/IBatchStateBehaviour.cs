namespace Spells.DataOriented.States
{
    public interface IBatchStateBehaviour<in T>
    {
        void StateStarted(T entity);
        void StateUpdate(T entity);
        void StateEnded(T entity);
    }
}