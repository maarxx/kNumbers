using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    using UnityEngine;

    public class PawnColumnWorker_Pain : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn) => pawn.health.hediffSet.PainTotal.ToStringPercent();

        protected override string GetTip(Pawn pawn) => GetPainTip(pawn);

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            GUI.color = HealthCardUtility.GetPainLabel(pawn).Second;
            base.DoCell(rect, pawn, table);
            GUI.color = Color.white;
        }

        private static string GetPainTip(Pawn pawn) => PainCausingHediffs(pawn).ToCommaList();

        private static IEnumerable<string> PainCausingHediffs(Pawn pawn) => (from t in pawn.health.hediffSet.hediffs where t.PainFactor != 1f || t.PainOffset != 0f select t.Part?.LabelCap + ": " + t.LabelCap).ToList();

        public override int GetMinWidth(PawnTable table) => base.GetMinWidth(table) + 12;

        public override int Compare(Pawn a, Pawn b) => a.health.hediffSet.PainTotal.CompareTo(b.health.hediffSet.PainTotal);
    }
}
