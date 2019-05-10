namespace AI
{
    public enum AIState
    {
        Wandering,
        MovingToRange,

        SelectingSpellAndAdaptingPosition,
        PerformingCast,
        WaitingAfterCast,

        StartingMeleeAttack,
        WaitingMeleeAttackAnimation,
        EndingMeleeAttack
    }
}
