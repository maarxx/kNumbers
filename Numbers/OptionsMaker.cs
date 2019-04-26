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
    }
}
