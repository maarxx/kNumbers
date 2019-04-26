using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Numbers
{
    public class OptionsMaker
    {
        private readonly MainTabWindow_Numbers numbers;
        private readonly Numbers_Settings settings;
        private PawnTableDef pawnTable;

        public OptionsMaker(MainTabWindow_Numbers mainTabWindow)
        {
            numbers = mainTabWindow;
            settings = LoadedModManager.GetMod<Numbers>().GetSettings<Numbers_Settings>();
            pawnTable = mainTabWindow.pawnTableDef;
        }

        public List<FloatMenuOption> PresetOptionsMaker()
        {
            return new List<FloatMenuOption>
            {
                new FloatMenuOption("Numbers_SaveCurrentLayout".Translate(), Save),
                new FloatMenuOption("Numbers_LoadSavedLayout".Translate(), Load),
                new FloatMenuOption("Numbers_Presets.Load".Translate("Numbers_Presets.Medical".Translate()), MakeThisMedical),
                new FloatMenuOption("Numbers_Presets.Load".Translate("Numbers_Presets.Combat".Translate()), MakeThisCombat),
                new FloatMenuOption("Numbers_Presets.Load".Translate("Numbers_Presets.WorkTabPlus".Translate()), MakeThisWorkTabPlus),
                new FloatMenuOption("Numbers_Presets.Load".Translate("Numbers_Presets.ColonistNeeds".Translate()), MakeThisColonistNeeds),
                new FloatMenuOption("Numbers_SetAsDefault".Translate(), SetAsDefault,
                        extraPartWidth: 29f,
                        extraPartOnGUI: (Rect rect)
                            => Numbers_Utility.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2,
                                "Numbers_SetAsDefaultExplanation".Translate(pawnTable.LabelCap))),
                new FloatMenuOption("Numbers_LoadDefault".Translate(), LoadDefault),
            };
        }

        public List<FloatMenuOption> OtherOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();


            List<PawnColumnDef> pcdList = new List<PawnColumnDef>
            {
                DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Race"),
            };

            list.AddRange(pcdList.Select(pcd => new FloatMenuOption("Race".Translate(), () => AddPawnColumnAtBestPositionAndRefresh(pcd))));


            //equipment bearers
            //array search is easier to type than if (PawnTableDef == X || PawnTableDef == Y etc etc)
            if (new[] { NumbersDefOf.Numbers_MainTable,
                        NumbersDefOf.Numbers_Prisoners,
                        NumbersDefOf.Numbers_Enemies,
                        NumbersDefOf.Numbers_Corpses
                      }.Contains(pawnTable))
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Equipment"),
                };

                list.AddRange(pcdList.Select(pcd => new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd), () => AddPawnColumnAtBestPositionAndRefresh(pcd))));

                foreach (PawnColumnDef pcd in pcdList)
                {
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                                () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            //all living things
            if (!new[] { NumbersDefOf.Numbers_AnimalCorpses, NumbersDefOf.Numbers_Corpses, }.Contains(pawnTable))
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

            if (pawnTable == NumbersDefOf.Numbers_Prisoners)
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

            if (pawnTable == NumbersDefOf.Numbers_Animals)
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

            if (pawnTable == NumbersDefOf.Numbers_MainTable)
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

            if (pawnTable == NumbersDefOf.Numbers_WildAnimals)
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
            if (new[] { NumbersDefOf.Numbers_AnimalCorpses, NumbersDefOf.Numbers_Corpses, }.Contains(pawnTable))
            {
                List<PawnColumnDef> pcdList = new List<PawnColumnDef>
                {
                    DefDatabase<PawnColumnDef>.GetNamedSilentFail("Numbers_Forbidden"),
                };
                foreach (var pcd in pcdList)
                {
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pcd),
                        () => AddPawnColumnAtBestPositionAndRefresh(pcd)));
                }
            }

            return list;
        }

        private List<FloatMenuOption> General()
        {

        }

        //generic that takes any List with type of Def.
        public List<FloatMenuOption> OptionsMakerFloatMenu<T>(in List<T> listOfDefs, in List<PawnColumnDef> optionalList = null) where T : Def
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            if (optionalList != null)
            {
                foreach (PawnColumnDef pawnColumnDef in optionalList)
                {
                    void Action()
                    {
                        this.AddPawnColumnAtBestPositionAndRefresh(pawnColumnDef);
                    }
                    list.Add(new FloatMenuOption(TryGetBestPawnColumnDefLabel(pawnColumnDef), Action));
                }
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

            return list;
        }

        public FloatMenu PawnSelectOptionsMaker()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (KeyValuePair<PawnTableDef, Func<Pawn, bool>> filter in WorldComponent_Numbers.PrimaryFilter)
            {
                void Action()
                {
                    if (filter.Value != MainTabWindow_Numbers.filterValidator.First())
                    {
                        if (Find.World.GetComponent<WorldComponent_Numbers>().sessionTable.TryGetValue(filter.Key, out List<PawnColumnDef> listPawnColumDef))
                            pawnTable.columns = listPawnColumDef;
                        else
                            pawnTable = filter.Key;

                        numbers.UpdateFilter();
                        numbers.Notify_ResolutionChanged();
                    }
                }
                list.Add(new FloatMenuOption(filter.Key.label, Action));
            }
            return new FloatMenu(list);
        }


        private void Save()
        {
            //not actually saved like this, just the easiest way to pass it around
            PawnTableDef ptdPawnTableDef = new PawnTableDef
            {
                columns = pawnTable.columns,
                modContentPack = pawnTable.modContentPack,
                workerClass = pawnTable.workerClass,
                defName = pawnTable.defName,
                label = "NumbersTable" + Rand.Range(0, 10000),
            };
            Find.WindowStack.Add(new Dialog_IHaveToCreateAnEntireFuckingDialogForAGODDAMNOKAYBUTTONFFS(ref ptdPawnTableDef));
        }

        private void Load()
        {
            List<FloatMenuOption> loadOptions = new List<FloatMenuOption>();
            foreach (string tableDefToBe in settings.storedPawnTableDefs)
            {
                void ApplySetting()
                {
                    PawnTableDef ptD = HorribleStringParsersForSaving.TurnCommaDelimitedStringIntoPawnTableDef(tableDefToBe);

                    pawnTable = DefDatabase<PawnTableDef>.GetNamed(ptD.defName);
                    pawnTable.columns = ptD.columns;

                    numbers.UpdateFilter();
                    numbers.RefreshAndStoreSessionInWorldComp();
                }
                string label = tableDefToBe.Split(',')[1] == "Default" ? tableDefToBe.Split(',')[0].Split('_')[1] + " (" + tableDefToBe.Split(',')[1] + ")" : tableDefToBe.Split(',')[1];
                loadOptions.Add(new FloatMenuOption(label, ApplySetting));
            }

            if (loadOptions.NullOrEmpty())
                loadOptions.Add(new FloatMenuOption("Numbers_NothingSaved".Translate(), null));

            Find.WindowStack.Add(new FloatMenu(loadOptions));
        }

        private void MakeThisMedical()
        {
            pawnTable = NumbersDefOf.Numbers_MainTable;
            pawnTable.columns = new List<PawnColumnDef>(StaticConstructorOnGameStart.medicalPreset);
            numbers.UpdateFilter();
            numbers.Notify_ResolutionChanged();
        }

        private void MakeThisCombat()
        {
            pawnTable = NumbersDefOf.Numbers_MainTable;
            pawnTable.columns = new List<PawnColumnDef>(StaticConstructorOnGameStart.combatPreset);
            numbers.UpdateFilter();
            numbers.Notify_ResolutionChanged();
        }

        private void MakeThisWorkTabPlus()
        {
            pawnTable = NumbersDefOf.Numbers_MainTable;
            pawnTable.columns = new List<PawnColumnDef>(StaticConstructorOnGameStart.workTabPlusPreset);
            numbers.UpdateFilter();
            numbers.Notify_ResolutionChanged();
        }

        private void MakeThisColonistNeeds()
        {
            pawnTable = NumbersDefOf.Numbers_MainTable;
            pawnTable.columns = new List<PawnColumnDef>(StaticConstructorOnGameStart.colonistNeedsPreset);
            numbers.UpdateFilter();
            numbers.Notify_ResolutionChanged();
        }

        private void SetAsDefault()
        {
            string pawnTableDeftoSave = HorribleStringParsersForSaving.TurnPawnTableDefIntoCommaDelimitedString(pawnTable, true);
            settings.StoreNewPawnTableDef(pawnTableDeftoSave);
        }

        private void LoadDefault()
        {
            bool foundSomething = false;
            foreach (string tableDefToBe in settings.storedPawnTableDefs)
            {
                string[] ptdToBe = tableDefToBe.Split(',');
                if (ptdToBe[1] == "Default" && pawnTable.defName == ptdToBe[0])
                {
                    foundSomething = true;
                    PawnTableDef ptD = HorribleStringParsersForSaving.TurnCommaDelimitedStringIntoPawnTableDef(tableDefToBe);

                    pawnTable = DefDatabase<PawnTableDef>.GetNamed(ptD.defName);
                    pawnTable.columns = ptD.columns;
                    numbers.UpdateFilter();
                    numbers.RefreshAndStoreSessionInWorldComp();
                    break; //there's only one default anyway.
                }
            }
            if (!foundSomething)
                Messages.Message("Numbers_NoDefaultStoredForThisView".Translate(), MessageTypeDefOf.RejectInput);
        }

        private static string TryGetBestPawnColumnDefLabel(PawnColumnDef pcd)
            => pcd == null
                ? string.Empty
                    : pcd.label.NullOrEmpty()
                        ? pcd.headerTip.NullOrEmpty()
                            ? pcd.defName
                        : pcd.headerTip
                    : pcd.LabelCap; //return labelcap if available, headertip if not, defName as last resort.

        private void AddPawnColumnAtBestPositionAndRefresh(PawnColumnDef pcd)
        {
            if (pcd == null)
                return;
            int lastIndex = pawnTable.columns.FindLastIndex(x => x.Worker is PawnColumnWorker_RemainingSpace);
            pawnTable.columns.Insert(Mathf.Max(1, lastIndex), pcd);

            numbers.RefreshAndStoreSessionInWorldComp();
        }

        private static readonly Func<PawnColumnDef, bool> pcdValidator = pcd => !(pcd.Worker is PawnColumnWorker_Gap)
                                && !(pcd.Worker is PawnColumnWorker_Label) && !(pcd.Worker is PawnColumnWorker_RemainingSpace)
                                && !(pcd.Worker is PawnColumnWorker_CopyPaste) && !(pcd.Worker is PawnColumnWorker_MedicalCare)
                                && !(pcd.Worker is PawnColumnWorker_Timetable) || (!(pcd.label.NullOrEmpty() && pcd.HeaderIcon == null)
                                && !pcd.HeaderInteractable);
        //basically all that are already present, don't have an interactable header, and uh
    }
}
