using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;

namespace Numbers
{
    public class MainTabWindow_Numbers : MainTabWindow_PawnTable
    {
        public const float buttonWidth = 110f;
        public const float buttonHeight = 35f;
        public const float buttonGap = 4f;
        public const float extraTopSpace = 83f;

        public static List<Func<Pawn, bool>> filterValidator = new List<Func<Pawn, bool>>
                                                        { Find.World.GetComponent<WorldComponent_Numbers>().primaryFilter.Value };

        private readonly IEnumerable<StatDef> pawnHumanlikeStatDef;
        private readonly IEnumerable<StatDef> pawnAnimalStatDef;
        private readonly IEnumerable<StatDef> corpseStatDef;
        private readonly IEnumerable<NeedDef> pawnHumanlikeNeedDef;
        private readonly IEnumerable<NeedDef> pawnAnimalNeedDef;

        private readonly OptionsMaker optionsMaker;

        //Code style: Use GetNamedSilentFail in cases where there is null-handling, so any columns that get run through TryGetBestPawnColumnDefLabel() or AddPawnColumnAtBestPositionAndRefresh() can silently fail.
        //Use GetNamed anywhere a null column would throw a null ref.
        private static readonly string workTabName = DefDatabase<MainButtonDef>.GetNamed("Work").ShortenedLabelCap;

        private IEnumerable<StatDef> StatDefs => PawnTableDef.Ext().Corpse ? corpseStatDef :
                        PawnTableDef.Ext().Animallike ? pawnAnimalStatDef : pawnHumanlikeStatDef;

        private IEnumerable<NeedDef> NeedDefs => PawnTableDef.Ext().Animallike ? pawnAnimalNeedDef : pawnHumanlikeNeedDef;

        //ctor to populate lists.
        public MainTabWindow_Numbers()
        {
            optionsMaker = new OptionsMaker(this);

            MethodInfo statsToDraw = typeof(StatsReportUtility).GetMethod("StatsToDraw",
                                                                          BindingFlags.NonPublic | BindingFlags.Static |
                                                                          BindingFlags.InvokeMethod, null,
                                                                          new[] { typeof(Thing) }, null);

            Pawn tmpPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.AncientSoldier, Faction.OfPlayerSilentFail);

            if (statsToDraw != null)
            {
                pawnHumanlikeStatDef =
                    ((IEnumerable<StatDrawEntry>)statsToDraw.Invoke(null, new[] { tmpPawn }))
                   .Concat(tmpPawn.def.SpecialDisplayStats(StatRequest.For(tmpPawn)))
                   .Where(s => s.ShouldDisplay && s.stat != null)
                   .Select(s => s.stat)
                   .OrderBy(stat => stat.LabelCap);

                tmpPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Thrumbo);

                pawnAnimalNeedDef = tmpPawn.needs.AllNeeds.Where(x => x.def.showOnNeedList).Select(x => x.def);

                pawnAnimalStatDef =
                    ((IEnumerable<StatDrawEntry>)statsToDraw.Invoke(null, new[] { tmpPawn }))
                   .Where(s => s.ShouldDisplay && s.stat != null)
                   .Select(s => s.stat)
                   .OrderBy(stat => stat.LabelCap);

                Corpse corpse = (Corpse)ThingMaker.MakeThing(tmpPawn.RaceProps.corpseDef);
                corpse.InnerPawn = tmpPawn;

                corpseStatDef = ((IEnumerable<StatDrawEntry>)statsToDraw.Invoke(null, new[] { corpse }))
                               .Concat(tmpPawn.def.SpecialDisplayStats(StatRequest.For(tmpPawn)))
                               .Where(s => s.ShouldDisplay && s.stat != null)
                               .Select(s => s.stat)
                               .OrderBy(stat => stat.LabelCap);
            }
            else
                Log.Error("ReflectionTypeLoadException in Numbers: statsToDraw was null. Please contact mod author.");

            pawnHumanlikeNeedDef = DefDatabase<NeedDef>.AllDefsListForReading;

            PawnTableDef defaultTable = WorldComponent_Numbers.PrimaryFilter.First().Key;
            if (Find.World.GetComponent<WorldComponent_Numbers>().sessionTable.TryGetValue(defaultTable, out List<PawnColumnDef> list))
                pawnTableDef.columns = list;

            UpdateFilter();
        }

        protected internal PawnTableDef pawnTableDef = NumbersDefOf.Numbers_MainTable;

        protected override PawnTableDef PawnTableDef => pawnTableDef;

        protected override IEnumerable<Pawn> Pawns
        {
            get
            {
                var corpseList = Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).Cast<Corpse>();

                foreach (Corpse corpse in corpseList)
                {
                    if (filterValidator.All(validator => validator(corpse.InnerPawn)))
                        yield return corpse.InnerPawn;
                }

                foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
                {
                    if (filterValidator.All(validator => validator(pawn)))
                        yield return pawn;
                }
            }
        }

        protected override float ExtraTopSpace => extraTopSpace;

        public override void DoWindowContents(Rect rect)
        {
            float x = 0f;
            Text.Font = GameFont.Small;

            //pawn selector
            Rect sourceButton = new Rect(x, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(sourceButton, PawnTableDef.label))
            {
                Find.WindowStack.Add(optionsMaker.PawnSelectOptionsMaker());
            }
            x += buttonWidth + buttonGap;

            TooltipHandler.TipRegion(sourceButton, new TipSignal("koisama.Numbers.ClickToToggle".Translate(), sourceButton.GetHashCode()));

            //stats
            Rect addColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(addColumnButton, "TabStats".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(optionsMaker.OptionsMakerFloatMenu(StatDefs)));
            }
            x += buttonWidth + buttonGap;

            //worktypes
            if (PawnTableDef == NumbersDefOf.Numbers_MainTable)
            {
                Rect workTypeColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(workTypeColumnButton, workTabName))
                {
                    Find.WindowStack.Add(new FloatMenu(optionsMaker.OptionsMakerFloatMenu(DefDatabase<PawnColumnDef>.AllDefsListForReading.Where(pcd => pcd.workType != null).Reverse())));
                }
                x += buttonWidth + buttonGap;
            }

            //skills
            if (new[] { NumbersDefOf.Numbers_Enemies, NumbersDefOf.Numbers_Prisoners, NumbersDefOf.Numbers_MainTable }.Contains(PawnTableDef))
            {
                Rect skillColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(skillColumnButton, "Skills".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(optionsMaker.OptionsMakerFloatMenu(DefDatabase<SkillDef>.AllDefsListForReading)));
                }
                x += buttonWidth + buttonGap;
            }

            //needs btn (for living things)
            if (!new[] { NumbersDefOf.Numbers_AnimalCorpses, NumbersDefOf.Numbers_Corpses, }.Contains(PawnTableDef))
            {
                Rect needsColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(needsColumnButton, "TabNeeds".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(optionsMaker.OptionsMakerFloatMenu(NeedDefs)));
                }
                x += buttonWidth + buttonGap;
            }

            //cap btn (for living things)
            if (!new[] { NumbersDefOf.Numbers_AnimalCorpses, NumbersDefOf.Numbers_Corpses, }.Contains(PawnTableDef))
            {
                Rect capacityColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(capacityColumnButton, "TabHealth".Translate()))
                {
                    List<PawnColumnDef> optionalList = new List<PawnColumnDef>();

                    if (new[] { NumbersDefOf.Numbers_MainTable, NumbersDefOf.Numbers_Prisoners, NumbersDefOf.Numbers_Animals, }.Contains(PawnTableDef))
                    {
                        optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("MedicalCare"));
                        optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Operations"));

                        if (PawnTableDef == NumbersDefOf.Numbers_MainTable)
                            optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_SelfTend"));
                    }
                    optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_HediffList"));
                    optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Pain"));
                    optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Bleedrate"));
                    optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_NeedsTreatment"));
                    optionalList.Add(DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_DiseaseProgress"));

                    var tmp = optionsMaker.OptionsMakerFloatMenu(DefDatabase<PawnCapacityDef>.AllDefsListForReading)
                                          .Concat(optionsMaker.OptionsMakerFloatMenu(optionalList));

                    Find.WindowStack.Add(new FloatMenu(tmp.ToList()));
                }
                x += buttonWidth + buttonGap;
            }

            //records btn
            Rect recordsColumnBtn = new Rect(x, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(recordsColumnBtn, "TabRecords".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(optionsMaker.OptionsMakerFloatMenu(DefDatabase<RecordDef>.AllDefsListForReading)));
            }
            x += buttonWidth + buttonGap;

            //other btn
            Rect otherColumnBtn = new Rect(x, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(otherColumnBtn, "MiscRecordsCategory".Translate()))
            {
                Find.WindowStack.Add(optionsMaker.OtherOptionsMaker());
            }
            x += buttonWidth + buttonGap;

            float startPositionOfPresetsButton = Mathf.Max(rect.xMax - buttonWidth - Margin, x);
            Rect addPresetBtn = new Rect(startPositionOfPresetsButton, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(addPresetBtn, "koisama.Numbers.SetPresetLabel".Translate()))
            {
                Find.WindowStack.Add(optionsMaker.PresetOptionsMaker());
            }

            // row count:
            Rect thingCount = new Rect(3f, 40f, 200f, 30f);
            Widgets.Label(thingCount, "koisama.Numbers.Count".Translate() + ": " + Pawns.Count());

            base.DoWindowContents(rect);
        }

        public override void PostOpen()
        {
            UpdateFilter();
            base.PostOpen();
            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
        }


        public void RefreshAndStoreSessionInWorldComp()
        {
            SetDirty();
            Notify_ResolutionChanged();
            Find.World.GetComponent<WorldComponent_Numbers>().sessionTable[PawnTableDef] = PawnTableDef.columns;
        }

        public void UpdateFilter()
        {
            filterValidator.Clear();
            filterValidator.Insert(0, WorldComponent_Numbers.PrimaryFilter[PawnTableDef]);
        }
    }
}
