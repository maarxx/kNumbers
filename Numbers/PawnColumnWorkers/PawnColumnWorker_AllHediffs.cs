namespace Numbers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using RimWorld;
    using UnityEngine;
    using Verse;

    [StaticConstructorOnStartup]
    public class PawnColumnWorker_AllHediffs : PawnColumnWorker_Icon
    {
        private static readonly Texture2D IconTendedWell = ContentFinder<Texture2D>.Get("UI/Icons/Trainables/Rescue");

        protected override Texture2D GetIconFor(Pawn pawn) => VisibleHediffs(pawn).Any() ? IconTendedWell : null;

        protected override string GetIconTip(Pawn pawn)
        {
            StringBuilder icontipBuilder = new StringBuilder();
            foreach (IGrouping<BodyPartRecord, Hediff> diffs in VisibleHediffGroupsInOrder(pawn))
            {
                foreach (IGrouping<int, Hediff> current in diffs.GroupBy(x => x.UIGroupKey))
                {
                    int    num4 = current.Count();
                    string text = current.First().LabelCap;
                    if (num4 != 1)
                    {
                        text = text + " x" + num4;
                    }
                    icontipBuilder.AppendWithComma(text);
                }
            }
            return icontipBuilder.ToString();
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return VisibleHediffs(a).Count().CompareTo(VisibleHediffs(b).Count());
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);
            GUI.color = Color.cyan;
            float   scale          = 0.7f;
            Vector2 headerIconSize = new Vector2(StaticConstructorOnGameStart.Plus.width * scale, StaticConstructorOnGameStart.Plus.height * scale);
            int     num            = (int)((rect.width - headerIconSize.x) / 4f);
            Rect    position       = new Rect(rect.x + num, rect.yMin + StaticConstructorOnGameStart.Tame.height, headerIconSize.x, headerIconSize.y);
            GUI.DrawTexture(position, StaticConstructorOnGameStart.Plus);
            GUI.color = Color.white;
        }

        //COPYYYYYY PASTE! Yum decompiler spaghetti.
        private static IEnumerable<Hediff> VisibleHediffs(Pawn pawn)
        {
            List<Hediff_MissingPart> mpca = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            foreach (Hediff_MissingPart t in mpca)
            {
                yield return t;
            }
            IEnumerable<Hediff> visibleDiffs = pawn.health.hediffSet.hediffs.Where(d => !(d is Hediff_MissingPart) && d.Visible);

            foreach (Hediff diff in visibleDiffs)
            {
                yield return diff;
            }
        }

        private static IEnumerable<IGrouping<BodyPartRecord, Hediff>> VisibleHediffGroupsInOrder(Pawn pawn) =>
            VisibleHediffs(pawn).GroupBy(x => x.Part).OrderByDescending(x => GetListPriority(x.First().Part));

        private static float GetListPriority(BodyPartRecord rec) => rec == null ? 9999999f : (int) rec.height * 10000 + rec.coverageAbsWithChildren;
    }
}
