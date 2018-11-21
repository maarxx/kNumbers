using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using System.Reflection;
using Harmony;

namespace Numbers
{
    public class PawnColumnWorker_OperationDropDown : PawnColumnWorker
    {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (Widgets.ButtonText(rect, "AddBill".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new FloatMenu(RecipeOptionsMaker(pawn, pawn)));
            }
            UIHighlighter.HighlightOpportunity(rect, "AddBill");
        }

        public override int GetMinCellHeight(Pawn pawn)
        {
            return 30;
        }

        public override int GetMinWidth(PawnTable table)
        {
            return 150;
        }

        //COPY PASTA AHOOOOY

        private static List<FloatMenuOption> RecipeOptionsMaker(Pawn pawn, Thing thingForMedBills)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            MethodInfo genSurgOp = typeof(HealthCardUtility).GetMethod("GenerateSurgeryOption", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod);

            foreach (RecipeDef current in thingForMedBills.def.AllRecipes)
            {
                if (current.AvailableNow)
                {
                    IEnumerable<ThingDef> enumerable = current.PotentiallyMissingIngredients(null, thingForMedBills.Map);
                    if (!enumerable.Any((ThingDef x) => x.isTechHediff))
                    {
                        if (!enumerable.Any((ThingDef x) => x.IsDrug))
                        {
                            if (!enumerable.Any<ThingDef>() || !current.dontShowIfAnyIngredientMissing)
                            {
                                if (current.targetsBodyPart)
                                {
                                    foreach (BodyPartRecord current2 in current.Worker.GetPartsToApplyOn(pawn, current))
                                    {
                                        list.Add((FloatMenuOption)genSurgOp.Invoke(null, new object[] { pawn, thingForMedBills, current, enumerable, current2 }));

                                        //HealthCardUtility.GenerateSurgeryOption(pawn, thingForMedBills, current, enumerable, current2));
                                    }
                                }
                                else
                                {
                                    list.Add( (FloatMenuOption)genSurgOp.Invoke(null, new object[] { pawn, thingForMedBills, current, enumerable, null }));

                                    //HealthCardUtility.GenerateSurgeryOption(pawn, thingForMedBills, current, enumerable, null));
                                }
                            }
                        }
                    }
                }
            }
            return list;
        }
    }
}
