namespace Numbers
{
    using RimWorld;
    using UnityEngine;
    using Verse;

    public class PawnColumnWorker_Race : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
        {
            return pawn.kindDef.race.LabelCap ?? string.Empty;
        }

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), 80);
        }

        public override int Compare(Pawn a, Pawn b) => a.kindDef.race.LabelCap.CompareTo(b.kindDef.race.LabelCap);
    }
}
