using RimWorld;
using UnityEngine;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_Capacity : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
            => pawn.health.capacities.GetLevel(this.def.Ext().capacity).ToStringPercent();

        protected override string GetTip(Pawn pawn)
            => HealthCardUtility.GetPawnCapacityTip(pawn, this.def.Ext().capacity);

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            GUI.color = HealthCardUtility.GetEfficiencyLabel(pawn, this.def.Ext().capacity).Second;
            base.DoCell(rect, pawn, table);
            GUI.color = Color.white;
        }

        public override int GetMinWidth(PawnTable table)
            => base.GetMinWidth(table) + 8; //based on Sight column.

        public override int Compare(Pawn a, Pawn b)
            => a.health.capacities.GetLevel(this.def.Ext().capacity)
            .CompareTo(b.health.capacities.GetLevel(this.def.Ext().capacity));

        public override int GetMinHeaderHeight(PawnTable table)
            => Mathf.CeilToInt(Text.CalcSize(this.def.LabelCap.WordWrapAt(this.GetMinWidth(table))).y);
    }
}
