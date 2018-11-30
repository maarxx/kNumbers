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

        private readonly Numbers_Settings settings;

        public static List<Func<Pawn, bool>> filterValidator = new List<Func<Pawn, bool>>
                                                        { Find.World.GetComponent<WorldComponent_Numbers>().primaryFilter.Value };

        private readonly List<StatDef> pawnHumanlikeStatDef;
        private readonly List<StatDef> pawnAnimalStatDef;
        private readonly List<StatDef> corpseStatDef;
        private readonly List<NeedDef> pawnHumanlikeNeedDef;
        private readonly List<NeedDef> pawnAnimalNeedDef;

        //Code style: GetNamedSilentFail in cases where there is null-handling, so any columns that get run through TryGetBestPawnColumnDefLabel() or AddPawnColumnAtBestPositionAndRefresh() can silently fail.
        //GetNamed anywhere a null column would through a null ref.
        private static readonly string workTabName = DefDatabase<MainButtonDef>.GetNamed("Work").ShortenedLabelCap;

        private List<StatDef> StatDefs => PawnTableDef.Ext().Corpse ? corpseStatDef :
                        PawnTableDef.Ext().Animallike ? pawnAnimalStatDef : pawnHumanlikeStatDef;

        private List<NeedDef> NeedDefs => PawnTableDef.Ext().Animallike ? pawnAnimalNeedDef : pawnHumanlikeNeedDef;

        //ctor to populate lists.
        public MainTabWindow_Numbers()
        {
            MethodInfo statsToDraw = typeof(StatsReportUtility).GetMethod("StatsToDraw",
                                                                          BindingFlags.NonPublic | BindingFlags.Static |
                                                                          BindingFlags.InvokeMethod, null,
                                                                          new [] { typeof(Thing) }, null);

            Pawn tmpPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.AncientSoldier, Faction.OfPlayerSilentFail);

            if (statsToDraw != null)
            {
                pawnHumanlikeStatDef =
                    ((IEnumerable<StatDrawEntry>)statsToDraw.Invoke(null, new[] { tmpPawn }))
                   .Concat(tmpPawn.def.SpecialDisplayStats(StatRequest.For(tmpPawn)))
                   .Where(s => s.ShouldDisplay && s.stat != null)
                   .Select(s => s.stat).OrderBy(stat => stat.LabelCap).ToList();

                tmpPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Thrumbo);

                pawnAnimalStatDef =
                    ((IEnumerable<StatDrawEntry>)statsToDraw.Invoke(null, new[] { tmpPawn }))
                   .Where(s => s.ShouldDisplay && s.stat != null)
                   .Select(s => s.stat).OrderBy(stat => stat.LabelCap).ToList();

                Corpse corpse = (Corpse)ThingMaker.MakeThing(tmpPawn.RaceProps.corpseDef);
                corpse.InnerPawn = tmpPawn;

                corpseStatDef = ((IEnumerable<StatDrawEntry>)statsToDraw.Invoke(null, new[] { corpse }))
                               .Concat(tmpPawn.def.SpecialDisplayStats(StatRequest.For(tmpPawn)))
                               .Where(s => s.ShouldDisplay && s.stat != null)
                               .Select(s => s.stat).OrderBy(stat => stat.LabelCap).ToList();
            }

            pawnHumanlikeNeedDef = DefDatabase<NeedDef>.AllDefsListForReading;
            pawnAnimalNeedDef = tmpPawn.needs.AllNeeds.Where(x => x.def.showOnNeedList).Select(x => x.def).ToList();

            PawnTableDef defaultTable = WorldComponent_Numbers.PrimaryFilter.First().Key;
            if (Find.World.GetComponent<WorldComponent_Numbers>().sessionTable.TryGetValue(defaultTable, out List<PawnColumnDef> list))
                pawnTableDef.columns = list;

            settings = LoadedModManager.GetMod<Numbers>().GetSettings<Numbers_Settings>();
            UpdateFilter();
        }

        protected internal PawnTableDef pawnTableDef = NumbersDefOf.Numbers_MainTable;

        protected override PawnTableDef PawnTableDef => pawnTableDef;

        protected override IEnumerable<Pawn> Pawns
        {
            get
            {
                var corpseList = Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);

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

        protected override float ExtraTopSpace => 83f;

        public override void DoWindowContents(Rect rect)
        {
            float x = 0f;
            Text.Font = GameFont.Small;

            //pawn selector
            Rect sourceButton = new Rect(x, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(sourceButton, PawnTableDef.label))
            {
                PawnSelectOptionsMaker();
            }
            x += buttonWidth + buttonGap;

            TooltipHandler.TipRegion(sourceButton, new TipSignal("koisama.Numbers.ClickToToggle".Translate(), sourceButton.GetHashCode()));

            //stats
            Rect addColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(addColumnButton, "TabStats".Translate()))
            {
                OptionsMakerFloatMenu(StatDefs);
            }
            x += buttonWidth + buttonGap;

            //worktypes
            if (PawnTableDef == NumbersDefOf.Numbers_MainTable)
            {
                Rect workTypeColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(workTypeColumnButton, workTabName))
                {
                    OptionsMakerFloatMenu(DefDatabase<PawnColumnDef>.AllDefsListForReading.Where(pcd => pcd.workType != null).Reverse().ToList());
                }
                x += buttonWidth + buttonGap;
            }

            //skills
            if (new[] { NumbersDefOf.Numbers_Enemies, NumbersDefOf.Numbers_Prisoners, NumbersDefOf.Numbers_MainTable }.Contains(PawnTableDef))
            {
                Rect skillColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(skillColumnButton, "Skills".Translate()))
                {
                    OptionsMakerFloatMenu(DefDatabase<SkillDef>.AllDefsListForReading);
                }
                x += buttonWidth + buttonGap;
            }

            //needs btn (for living things)
            if (!new[] { NumbersDefOf.Numbers_AnimalCorpses, NumbersDefOf.Numbers_Corpses, }.Contains(PawnTableDef))
            {
                Rect needsColumnButton = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(needsColumnButton, "TabNeeds".Translate()))
                {
                    OptionsMakerFloatMenu(NeedDefs);
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

                    OptionsMakerFloatMenu(DefDatabase<PawnCapacityDef>.AllDefsListForReading, optionalList);
                }
                x += buttonWidth + buttonGap;
            }

            //records btn
            Rect recordsColumnBtn = new Rect(x, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(recordsColumnBtn, "TabRecords".Translate()))
            {
                OptionsMakerFloatMenu(DefDatabase<RecordDef>.AllDefsListForReading);
            }
            x += buttonWidth + buttonGap;

            //other btn
            if (PawnTableDef != NumbersDefOf.Numbers_AnimalCorpses)
            {
                Rect otherColumnBtn = new Rect(x, 0f, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(otherColumnBtn, "MiscRecordsCategory".Translate()))
                {
                    OtherOptionsMaker();
                }
                x += buttonWidth + buttonGap;
            }

            float startPositionOfPresetsButton = Mathf.Max(rect.xMax - buttonWidth - Margin, x);
            Rect addPresetBtn = new Rect(startPositionOfPresetsButton, 0f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(addPresetBtn, "koisama.Numbers.SetPresetLabel".Translate()))
            {
                PresetOptionsMaker();
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

        public void PawnSelectOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (KeyValuePair<PawnTableDef, Func<Pawn, bool>> filter in WorldComponent_Numbers.PrimaryFilter)
            {
                void Action()
                {
                    if (filter.Value != filterValidator.First())
                    {
                        pawnTableDef = filter.Key;
                        UpdateFilter();
                        Notify_ResolutionChanged();
                    }
                }
                list.Add(new FloatMenuOption(filter.Key.label, Action));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        //generic that takes any List with type of Def.
        private void OptionsMakerFloatMenu<T>(List<T> listOfDefs, List<PawnColumnDef> optionalList = null) where T : Def
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            if (optionalList != null)
                foreach (PawnColumnDef pawnColumnDef in optionalList)
                {
                    void Action()
                    {
                        this.AddPawnColumnAtBestPositionAndRefresh(pawnColumnDef);
                    }
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pawnColumnDef), Action));
                }

            foreach (var defCurrent in listOfDefs)
            {
                void Action()
                {
                    if (defCurrent is PawnColumnDef columnDef)
                    {
                        AddPawnColumnAtBestPositionAndRefresh(columnDef);
                    }
                    else
                    {
                        PawnColumnDef pcd = DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_" + defCurrent.GetType().ToString().Replace('.', '_') + "_" + defCurrent.defName);
                        AddPawnColumnAtBestPositionAndRefresh(pcd);
                    }
                }
                string label = defCurrent is PawnColumnDef worker ? worker.workType?.labelShort ?? worker.defName : defCurrent.LabelCap;
                list.Add(new FloatMenuOption(label, Action));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        private void PresetOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            void Save()
            {
                string name = "NumbersTable";
                //not actually saved like this, just the easiest way to pass it around
                PawnTableDef ptdPawnTableDef = new PawnTableDef
                {
                    columns = PawnTableDef.columns,
                    modContentPack = PawnTableDef.modContentPack,
                    workerClass = PawnTableDef.workerClass,
                    defName = PawnTableDef.defName,
                    label = name + Rand.Range(0, 10000),
                };
                Find.WindowStack.Add(new Dialog_IHaveToCreateAnEntireFuckingDialogForAGODDAMNOKAYBUTTONFFS(ref ptdPawnTableDef));
            }

            list.Add(new FloatMenuOption("Numbers_SaveCurrentLayout".Translate(), Save));

            void Load()
            {
                List<FloatMenuOption> loadOptions = new List<FloatMenuOption>();
                foreach (string tableDefToBe in settings.storedPawnTableDefs)
                {
                    void ApplySetting()
                    {
                        PawnTableDef ptD = HorribleStringParsersForSaving.TurnCommaDelimitedStringIntoPawnTableDef(tableDefToBe);

                        pawnTableDef = DefDatabase<PawnTableDef>.GetNamed(ptD.defName);
                        pawnTableDef.columns = ptD.columns;
                        this.UpdateFilter();
                        RefreshAndStoreSessionInWorldComp();
                    }
                    string label = tableDefToBe.Split(',')[1] == "Default" ? tableDefToBe.Split(',')[0].Split('_')[1] + " (" + tableDefToBe.Split(',')[1] + ")" : tableDefToBe.Split(',')[1];
                    loadOptions.Add(new FloatMenuOption(label, ApplySetting));
                }

                if (loadOptions.NullOrEmpty())
                    loadOptions.Add(new FloatMenuOption("Numbers_NothingSaved".Translate(), null));

                Find.WindowStack.Add(new FloatMenu(loadOptions));
            }
            list.Add(new FloatMenuOption("Numbers_LoadSavedLayout".Translate(), Load));

            void MakeThisMedical()
            {
                this.pawnTableDef = NumbersDefOf.Numbers_MainTable;
                PawnTableDef.columns = new List<PawnColumnDef>
                                       {
                                           DefDatabase<PawnColumnDef>.GetNamed("Label"),
                                           DefDatabase<PawnColumnDef>.GetNamed("MedicalCare"),
                                           DefDatabase<PawnColumnDef>.GetNamed("Numbers_SelfTend"),
                                           DefDatabase<PawnColumnDef>.GetNamed("Numbers_HediffList"),
                                           DefDatabase<PawnColumnDef>.GetNamed("Numbers_RimWorld_StatDef_MedicalSurgerySuccessChance"),
                                           DefDatabase<PawnColumnDef>.GetNamed("Numbers_RimWorld_StatDef_MedicalTendQuality"),
                                           DefDatabase<PawnColumnDef>.GetNamed("Numbers_RimWorld_StatDef_MedicalTendSpeed"),
                                           DefDatabase<PawnColumnDef>.GetNamed("Numbers_Bleedrate"),
                                           DefDatabase<PawnColumnDef>.GetNamed("Numbers_Pain"),
                                       };
                PawnTableDef.columns.AddRange(DefDatabase<PawnColumnDef>.AllDefsListForReading
                                                                        //.Where(pcd => pcd.workerClass == typeof(PawnColumnWorker_WorkPriority)) //disabled for Fluffy Compat.
                                                                        .Where(pcd => pcd.workType != null)
                                                                        .Where(x => x.workType.defName == "Patient" ||
                                                                                    x.workType.defName == "Doctor" ||
                                                                                    x.workType.defName == "PatientBedRest").Reverse());

                foreach (PawnCapacityDef defCurrent in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
                {
                    PawnColumnDef pcd = DefDatabase<PawnColumnDef>.GetNamed("Numbers_" + defCurrent.GetType().ToString().Replace('.', '_') + "_" + defCurrent.defName);
                    PawnTableDef.columns.Add(pcd);
                }
                PawnTableDef.columns.RemoveAll(x => x.defName == "Numbers_Verse_PawnCapacityDef_Metabolism"); //I need space
                PawnTableDef.columns.Add(DefDatabase<PawnColumnDef>.GetNamed("Numbers_NeedsTreatment"));
                PawnTableDef.columns.Add(DefDatabase<PawnColumnDef>.GetNamed("Numbers_Operations"));
                PawnTableDef.columns.Add(DefDatabase<PawnColumnDef>.GetNamed("Numbers_DiseaseProgress"));

                PawnTableDef.columns.Add(DefDatabase<PawnColumnDef>.GetNamed("RemainingSpace"));
                this.UpdateFilter();
                Notify_ResolutionChanged();
            }
            list.Add(new FloatMenuOption("Numbers_Presets.Load".Translate("Numbers_Presets.Medical".Translate()), MakeThisMedical));

            void MakeThisCombat()
            {
                this.pawnTableDef = NumbersDefOf.Numbers_MainTable;
                PawnTableDef.columns = new List<PawnColumnDef>();
                PawnTableDef.columns.AddRange(StaticConstructorOnGameStart.combatPreset);
                this.UpdateFilter();
                Notify_ResolutionChanged();
            }
            list.Add(new FloatMenuOption("Numbers_Presets.Load".Translate("Numbers_Presets.Combat".Translate()), MakeThisCombat));

            void MakeThisWorkTabPlus()
            {
                this.pawnTableDef = NumbersDefOf.Numbers_MainTable;
                PawnTableDef.columns = new List<PawnColumnDef>();
                PawnTableDef.columns.AddRange(StaticConstructorOnGameStart.workTabPlusPreset);
                this.UpdateFilter();
                Notify_ResolutionChanged();
            }
            list.Add(new FloatMenuOption("Numbers_Presets.Load".Translate("Numbers_Presets.WorkTabPlus".Translate()), MakeThisWorkTabPlus));

            void setAsDefault()
            {
                string pawnTableDeftoSave = HorribleStringParsersForSaving.TurnPawnTableDefIntoCommaDelimitedString(PawnTableDef, true);

                settings.StoreNewPawnTableDef(pawnTableDeftoSave);
            }
            list.Add(new FloatMenuOption("Numbers_SetAsDefault".Translate(), setAsDefault, extraPartWidth: 29f, extraPartOnGUI: (Rect rect) => Numbers_Utility.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2, "Numbers_SetAsDefaultExplanation".Translate(PawnTableDef.LabelCap))));

            void loadDefault()
            {
                bool foundSomething = false;
                foreach (string tableDefToBe in settings.storedPawnTableDefs)
                {
                    string[] ptdToBe = tableDefToBe.Split(',');
                    if (ptdToBe[1] == "Default" && PawnTableDef.defName == ptdToBe[0])
                    {
                        foundSomething = true;
                        PawnTableDef ptD = HorribleStringParsersForSaving.TurnCommaDelimitedStringIntoPawnTableDef(tableDefToBe);

                        pawnTableDef = DefDatabase<PawnTableDef>.GetNamed(ptD.defName);
                        pawnTableDef.columns = ptD.columns;
                        this.UpdateFilter();
                        RefreshAndStoreSessionInWorldComp();
                        break; //there's only one default anyway.
                    }
                }
                if (!foundSomething)
                    Messages.Message("Numbers_NoDefaultStoredForThisView".Translate(), MessageTypeDefOf.RejectInput);
            }
            list.Add(new FloatMenuOption("Numbers_LoadDefault".Translate(), loadDefault));

            Find.WindowStack.Add(new FloatMenu(list));
        }

        //other hardcoded options
        public void OtherOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            void AddRace()
            {
                PawnColumnDef pcd = DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Race");
                AddPawnColumnAtBestPositionAndRefresh(pcd);
            }
            list.Add(new FloatMenuOption("Race".Translate(), AddRace));

            //equipment bearers
            //array search is easier to type than if (PawnTableDef == X || PawnTableDef == Y etc etc)
            if (new[] { NumbersDefOf.Numbers_MainTable, NumbersDefOf.Numbers_Prisoners, NumbersDefOf.Numbers_Enemies, NumbersDefOf.Numbers_Corpses }.Contains(PawnTableDef))
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Equipment"),
                };

                foreach (PawnColumnDef pcd in pcdList)
                {
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                                () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            //all living things
            if (!new[] { NumbersDefOf.Numbers_AnimalCorpses, NumbersDefOf.Numbers_Corpses, }.Contains(PawnTableDef))
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Age"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_MentalState"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_JobCurrent"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_JobQueued"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_HediffList")
                };

                foreach (PawnColumnDef pcd in pcdList)
                {
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                                () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            if (PawnTableDef == NumbersDefOf.Numbers_Prisoners)
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_PrisonerInteraction"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_PrisonerRecruitmentDifficulty"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_PrisonerResistance"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("FoodRestriction"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Inventory"),
                };

                foreach (PawnColumnDef pcd in pcdList)
                {
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                                () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            if (PawnTableDef == NumbersDefOf.Numbers_Animals)
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Milkfullness"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_AnimalWoolGrowth"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_AnimalEggProgress"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Wildness"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_TameChance"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Inventory"),
                };

                IEnumerable<PawnColumnDef> pawnColumnDefs = pcdList.Concat(DefDatabase<PawnTableDef>.GetNamed("Animals").columns.Where(x => pcdValidator(x)));

                foreach (PawnColumnDef pcd in pawnColumnDefs)
                {
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                                () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            if (PawnTableDef == NumbersDefOf.Numbers_MainTable)
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Inspiration"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Inventory"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_SelfTend"),
                };
                // (assign + restrict).Where(validator) + pcdList
                foreach (var pcd in DefDatabase<PawnTableDef>.GetNamed("Assign").columns
                    .Concat(DefDatabase<PawnTableDef>.GetNamed("Restrict").columns).Where(x => pcdValidator(x))
                    .Concat(pcdList))
                {
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                                () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            if (PawnTableDef == NumbersDefOf.Numbers_WildAnimals)
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Wildness"),
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_TameChance"),
                };
                foreach (var pcd in DefDatabase<PawnTableDef>.GetNamed("Wildlife").columns.Where(x => pcdValidator(x)).Concat(pcdList))
                {
                    list.Add(new FloatMenuOption(pcd.defName,
                            () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            //all dead things
            if (new[] { NumbersDefOf.Numbers_AnimalCorpses, NumbersDefOf.Numbers_Corpses, }.Contains(PawnTableDef))
            {
                PawnColumnDef pcd = DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Forbidden");
                list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                            () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        private static string TryGetBestPawnColumnDefLabel(PawnColumnDef pcd) =>
            pcd == null ? string.Empty : pcd.label.NullOrEmpty() ? pcd.headerTip.NullOrEmpty() ? pcd.defName : pcd.headerTip : pcd.LabelCap; //return labelcap if available, headertip if not, defName as last resort.

        private void AddPawnColumnAtBestPositionAndRefresh(PawnColumnDef pcd)
        {
            if (pcd == null) return;
            int lastIndex = PawnTableDef.columns.FindLastIndex(x => x.Worker is PawnColumnWorker_RemainingSpace);
            PawnTableDef.columns.Insert(Mathf.Max(1, lastIndex), pcd);
            RefreshAndStoreSessionInWorldComp();
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

        private static readonly Func<PawnColumnDef, bool> pcdValidator = pcd => !(pcd.Worker is PawnColumnWorker_Gap) 
                                        && !(pcd.Worker is PawnColumnWorker_Label)     && !(pcd.Worker is PawnColumnWorker_RemainingSpace)
                                        && !(pcd.Worker is PawnColumnWorker_CopyPaste) && !(pcd.Worker is PawnColumnWorker_MedicalCare) 
                                        && !(pcd.Worker is PawnColumnWorker_Timetable) || (!(pcd.label.NullOrEmpty() && pcd.HeaderIcon == null)
                                        && !pcd.HeaderInteractable);
        //basically all that are already present, don't have an interactable header, and uh
    }
}
