namespace Spells.DataOriented.States
{
    public class SpellInfo
    {
        public SubSpellState State;
        public Spell Spell;
        public int Stacks;
        public Target CastSource;
        public Target OriginalCastTarget;
        public Target CastTarget;
        public int WaitingSubspells;

        public bool TargetsAreValid()
        {
            return CastSource.IsValid && OriginalCastTarget.IsValid;
        }
    }
}