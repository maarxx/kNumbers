using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_PrisonerResistance : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
        {
            return pawn.guest.Resistance.ToString("F1");
        }

        public override int Compare(Pawn a, Pawn b) => a.guest.Resistance.CompareTo(b.guest.Resistance);

        public override int GetMinHeaderHeight(PawnTable table) => Mathf.CeilToInt(Text.CalcSize(this.def.LabelCap.WordWrapAt(this.GetMinWidth(table))).y);
    }
}
