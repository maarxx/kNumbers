using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Numbers
{
    [StaticConstructorOnStartup]
    public class PawnColumnWorker_NeedsTreatment : PawnColumnWorker_Icon
    {
        private static readonly Texture2D IconTendedNeed = ContentFinder<Texture2D>.Get("UI/Icons/Medical/TendedNeed", true);
        private static readonly Texture2D IconTendedWell = ContentFinder<Texture2D>.Get("UI/Icons/Medical/TendedWell", true);

        protected override Texture2D GetIconFor(Pawn pawn) => pawn.health.HasHediffsNeedingTendByPlayer() ? IconTendedNeed : IconTendedWell;

        public override int Compare(Pawn a, Pawn b) =>
            a.health.hediffSet.GetHediffsTendable().Count().CompareTo(b.health.hediffSet.GetHediffsTendable().Count());

        protected override string GetIconTip(Pawn pawn) => pawn.health.hediffSet.GetHediffsTendable().Select(x => x.LabelCap).ToCommaList();

        public override int GetMinHeaderHeight(PawnTable table) => Mathf.CeilToInt(Text.CalcSize(this.def.LabelCap.WordWrapAt(this.GetMinWidth(table))).y);
    }
}
