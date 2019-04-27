namespace Numbers
{
    using RimWorld;
    using Verse;

    class PawnColumnWorker_Faction : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
            => pawn.Faction?.Name;
    }
}
