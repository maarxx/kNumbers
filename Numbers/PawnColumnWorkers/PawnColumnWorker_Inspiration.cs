using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    using UnityEngine;

    public class PawnColumnWorker_Inspiration : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn) => pawn.InspirationDef?.LabelCap;

        public override int Compare(Pawn a, Pawn b) => (a.Inspired ? a.InspirationDef.GetHashCode() : int.MinValue).CompareTo(b.Inspired ? b.InspirationDef.GetHashCode() : int.MinValue);
        
        protected override string GetTip(Pawn pawn)
        {
            int? inspirationTimeRemaining = (int?)((pawn.InspirationDef?.baseDurationDays - pawn.Inspiration?.AgeDays) * GenDate.TicksPerDay);

            return inspirationTimeRemaining.HasValue ? "ExpiresIn".Translate() + ": " + inspirationTimeRemaining.Value.ToStringTicksToPeriod() : string.Empty;
        }

        public override int GetMinWidth(PawnTable table) => Mathf.Max(base.GetMinWidth(table), 130);
    }
}
