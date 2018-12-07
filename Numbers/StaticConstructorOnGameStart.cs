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
        public static readonly Texture2D    DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete"),
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
                                          colonistNeedsPreset = new List<PawnColumnDef>();

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

            foreach (PawnColumnDef pawnColumnDef in DefDatabase<PawnColumnDef>.AllDefsListForReading.Where(x => !x.generated && x.defName.StartsWith("Numbers_") && !(x.Worker is PawnColumnWorker_AllHediffs || x.Worker is PawnColumnWorker_SelfTend))) //special treatment for those.
            {
                pawnColumnDef.headerTip += (pawnColumnDef.headerTip.NullOrEmpty() ? "" : "\n\n") + "Numbers_ColumnHeader_Tooltip".Translate();
            }

            foreach (PawnColumnDef pcd in DefDatabase<PawnTableDef>.GetNamed("Numbers_CombatPreset").columns)
            {
                combatPreset.Add(pcd);
            }

            foreach (PawnColumnDef pcd in DefDatabase<PawnTableDef>.GetNamed("Numbers_WorkTabPlusPreset").columns)
            {
                workTabPlusPreset.Add(pcd);
            }

            foreach (PawnColumnDef pcd in DefDatabase<PawnTableDef>.GetNamed("Numbers_ColonistNeedsPreset").columns)
            {
                colonistNeedsPreset.Add(pcd);
            }
        }
    }
}
