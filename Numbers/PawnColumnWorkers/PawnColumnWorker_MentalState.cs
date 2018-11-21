using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    using UnityEngine;

    public class PawnColumnWorker_MentalState : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
        {
            return pawn.MentalState?.InspectLine ?? string.Empty;
        }

        public override int Compare(Pawn a, Pawn b) => ((int?)a.MentalState?.def?.category ?? -1).CompareTo((int?)b.MentalState?.def?.category ?? -1);

        public override int GetMinHeaderHeight(PawnTable table) => Mathf.CeilToInt(Text.CalcSize(this.def.LabelCap.WordWrapAt(this.GetMinWidth(table))).y);
    }
}
