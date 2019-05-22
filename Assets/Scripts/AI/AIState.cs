namespace AI
{
    public enum AIState
    {
        Wandering,
        AggroMove,
        SpellIntention,
        MovingToSpellRange,
        PerformingCast,
        WaitingAfterCast,
        StartingMeleeAttack,
        WaitingMeleeAttackAnimation,
        EndingMeleeAttack
    }
}
