namespace Spells
{
    public enum SubSpellEvent
    {
        Started,
        AfterPreCastDelay,
        OnFire,
        AfterFinishedFire,
        Ended,
        ProjectileHit,
        ProjectileDestroy
    }

    public enum SpellEvent
    {
        StartedFiring,
        SubSpellCasted,
        FinishedFire,
        Aborted,
        Ended
    }
}