using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Numbers
{

    [StaticConstructorOnStartup]
    static class StaticConstructorOnGameStart
    {
        public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete"),
                                            Info = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton"),
                                            Predator = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Predator"),
                                            Tame = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Tame"),
                                            List = ContentFinder<Texture2D>.Get("UI/Buttons/DevRoot/ToggleTweak"),
                                            Plus = ContentFinder<Texture2D>.Get("UI/Icons/Trainables/Rescue"),
                                            IconImmune = ContentFinder<Texture2D>.Get("UI/Icons/Medical/IconImmune"),
                                            IconDead = ContentFinder<Texture2D>.Get("Things/Mote/BattleSymbols/Skull"),
                                            IconTendedWell = ContentFinder<Texture2D>.Get("UI/Icons/Medical/TendedWell"),
                                            IconTendedNeed = ContentFinder<Texture2D>.Get("UI/Icons/Medical/TendedNeed"),
                                            SortingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Sorting"),
                                            SortingDescendingIcon = ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending"),
                                            BarInstantMarkerTex = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarker");

        public static List<PawnColumnDef> combatPreset = new List<PawnColumnDef>(),
                                          workTabPlusPreset = new List<PawnColumnDef>(),
                                          colonistNeedsPreset = new List<PawnColumnDef>(),
                                          medicalPreset = new List<PawnColumnDef>();

        static StaticConstructorOnGameStart()
        {
            //add trainables to animal table
            PawnTableDef animalsTable = NumbersDefOf.Numbers_Animals;

            foreach (PawnColumnDef item in DefDatabase<PawnColumnDef>.AllDefsListForReading.Where(x => x.Worker is PawnColumnWorker_Trainable))
            {
                animalsTable.columns.Insert(animalsTable.columns.FindIndex(x => x.Worker is PawnColumnWorker_Checkbox) - 1, item);
            }

            //add remaining space to my PTDefs
            IEnumerable<PawnTableDef> allPawntableDefs = DefDatabase<PawnTableDef>.AllDefsListForReading.Where(x => x.HasModExtension<DefModExtension_PawnTableDefs>());

            PawnColumnDef remainingspace = DefDatabase<PawnColumnDef>.AllDefsListForReading.First(x => x.Worker is PawnColumnWorker_RemainingSpace);

            IEnumerable<PawnTableDef> ptsDfromPtDses = allPawntableDefs as PawnTableDef[] ?? allPawntableDefs.ToArray();

            foreach (PawnTableDef PTSDfromPTDs in ptsDfromPtDses)
            {
                PTSDfromPTDs.columns.Insert(PTSDfromPTDs.columns.Count, remainingspace);
            }

            foreach (PawnColumnDef pawnColumnDef in DefDatabase<PawnColumnDef>
                .AllDefsListForReading
                .Where(x => !x.generated
                        && x.defName.StartsWith("Numbers_")
                        && !(x.Worker is PawnColumnWorker_AllHediffs
                        || x.Worker is PawnColumnWorker_SelfTend))) //special treatment for those.
            {
                pawnColumnDef.headerTip += (pawnColumnDef.headerTip.NullOrEmpty() ? "" : "\n\n") + "Numbers_ColumnHeader_Tooltip".Translate();
            }

            combatPreset.AddRange(DefDatabase<PawnTableDef>.GetNamed("Numbers_CombatPreset").columns);
            workTabPlusPreset.AddRange(DefDatabase<PawnTableDef>.GetNamed("Numbers_WorkTabPlusPreset").columns);
            colonistNeedsPreset.AddRange(DefDatabase<PawnTableDef>.GetNamed("Numbers_ColonistNeedsPreset").columns);

            medicalPreset.AddRange(new List<PawnColumnDef>
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
                                       });
            medicalPreset.AddRange(DefDatabase<PawnColumnDef>.AllDefsListForReading
                                                                    .Where(pcd => pcd.workType != null)
                                                                    .Where(x => x.workType.defName == "Patient" ||
                                                                                x.workType.defName == "Doctor" ||
                                                                                x.workType.defName == "PatientBedRest").Reverse());

            medicalPreset
                .AddRange(DefDatabase<PawnCapacityDef>
                .AllDefsListForReading
                .Select(x => DefDatabase<PawnColumnDef>
                .GetNamed(HorribleStringParsersForSaving.CreateDefNameFromType(x))));

            medicalPreset.RemoveAll(x => x.defName == "Numbers_Verse_PawnCapacityDef_Metabolism"); //I need space
            medicalPreset.AddRange(new List<PawnColumnDef>
            {
                DefDatabase<PawnColumnDef>.GetNamed("Numbers_NeedsTreatment"),
                DefDatabase<PawnColumnDef>.GetNamed("Numbers_Operations"),
                DefDatabase<PawnColumnDef>.GetNamed("Numbers_DiseaseProgress"),
                DefDatabase<PawnColumnDef>.GetNamed("RemainingSpace"),
            });
        }
    }
}
