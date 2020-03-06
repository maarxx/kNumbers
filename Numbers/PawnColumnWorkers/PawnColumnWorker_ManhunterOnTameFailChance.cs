namespace Numbers
{
    using RimWorld;
    using UnityEngine;
    using Verse;

    public class PawnColumnWorker_ManhunterOnTameFailChance : PawnColumnWorker_Text
    {
        public static float IconPositionVertical = 35f;
        public static float IconPositionHorizontal = 5f;

        public override int Compare(Pawn a, Pawn b)
            => GetValue(a).CompareTo(GetValue(b));

        protected override string GetTextFor(Pawn pawn)
            => GetValue(pawn).ToStringPercent();

        protected override string GetTip(Pawn pawn)
            => "MessageAnimalManhuntsOnTameFailed".Translate(pawn.kindDef.GetLabelPlural().CapitalizeFirst(),
                                                             GetValue(pawn).ToStringPercent(), pawn.Named("ANIMAL"));

        private float GetValue(Pawn pawn)
            => pawn.RaceProps.manhunterOnTameFailChance;

        protected override string GetHeaderTip(PawnTable table)
            => "TameFailedRevengeChance".Translate() + "\n\n" + "Numbers_ColumnHeader_Tooltip".Translate();

        public override void DoHeader(Rect rect, PawnTable table)
        {
            float scale = 0.3f;
            base.DoHeader(rect, table);
            Vector2 headerIconSize = new Vector2(StaticConstructorOnGameStart.Tame.width, StaticConstructorOnGameStart.Tame.height) * scale;
            int num = (int)((rect.width - headerIconSize.x) / 2f);
            Rect position = new Rect(rect.x + num + IconPositionHorizontal, rect.yMax - StaticConstructorOnGameStart.Tame.height + IconPositionVertical, headerIconSize.x, headerIconSize.y);
            GUI.DrawTexture(position, StaticConstructorOnGameStart.Tame);
        }
    }
}
