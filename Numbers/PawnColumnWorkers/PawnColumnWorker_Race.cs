using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_Race : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
        {
            return pawn.kindDef.race.LabelCap ?? string.Empty;
        }

        public override int GetMinWidth(PawnTable table)
        {
            return UnityEngine.Mathf.Max(base.GetMinWidth(table), 80);
        }

        public override int Compare(Pawn a, Pawn b) => a.kindDef.race.LabelCap[0].CompareTo(b.kindDef.race.LabelCap[0]);
    }
}
