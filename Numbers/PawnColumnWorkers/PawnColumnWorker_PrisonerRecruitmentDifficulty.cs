using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    using UnityEngine;

    public class PawnColumnWorker_PrisonerRecruitmentDifficulty : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
            => pawn.RecruitDifficulty(Faction.OfPlayer).ToStringPercent();

        public override int Compare(Pawn a, Pawn b)
            => a.RecruitDifficulty(Faction.OfPlayer).CompareTo(b.RecruitDifficulty(Faction.OfPlayer));

        public override int GetMinHeaderHeight(PawnTable table)
            => Mathf.CeilToInt(Text.CalcSize(this.def.LabelCap.WordWrapAt(this.GetMinWidth(table))).y);
    }
}
