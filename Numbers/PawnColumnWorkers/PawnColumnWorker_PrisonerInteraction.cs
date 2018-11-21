using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Numbers
{
    using Verse.Sound;

    public class PawnColumnWorker_PrisonerInteraction : PawnColumnWorker
    {
        //for mods, like Prison Labour, that add more interactinodefs.
        private readonly int width = DefDatabase<PrisonerInteractionModeDef>.DefCount * 30;//AllDefsListForReading.Count * 30;
        private bool dragging;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.guest == null)
                return;
            
            GUI.BeginGroup(rect);
            float x = 0f;

            foreach (PrisonerInteractionModeDef current in from prisonerinteractionmode in DefDatabase<PrisonerInteractionModeDef>.AllDefsListForReading
                                                           orderby prisonerinteractionmode.listOrder
                                                           select prisonerinteractionmode)
            {
                DrawInteractionRadioButton(new Rect(x, 3f, 30f, 30f), pawn, current);
                TooltipHandler.TipRegion(new Rect(x, 0f, 30f, 30f), new TipSignal(current.LabelCap));
                x += 30f;
            }
            GUI.EndGroup();
        }

        private void DrawInteractionRadioButton(Rect rect, Pawn pawn, PrisonerInteractionModeDef prisonerInteraction)
        {
            //inspired by RimWorld.AreaAllowedGUI.DoAreaSelector(Rect rect, Pawn p, Area area)
            Widgets.RadioButton(rect.x, rect.y, pawn.guest.interactionMode == prisonerInteraction);
            {
                if (Input.GetMouseButtonUp(0))
                {
                    dragging = false;
                }
                if (!Input.GetMouseButtonDown(0))
                {
                    dragging = false;
                }
                if (Mouse.IsOver(rect))
                {
                    if (Input.GetMouseButton(0))
                    {
                        dragging = true;
                        pawn.guest.interactionMode = prisonerInteraction;
                    }
                    if (dragging && pawn.guest.interactionMode != prisonerInteraction)
                    {
                        pawn.guest.interactionMode = prisonerInteraction;
                        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
                    }
                }
            }
        }

        public override int Compare(Pawn a, Pawn b) => (a.guest?.interactionMode?.listOrder ?? 0).CompareTo(b.guest?.interactionMode?.listOrder ?? 0);

        public override int GetMinWidth(PawnTable table) => width; //Mathf.Max(base.GetMinWidth(table), 160);

        public override int GetMinHeaderHeight(PawnTable table) => Mathf.CeilToInt(Text.CalcSize(this.def.LabelCap.WordWrapAt(this.GetMinWidth(table))).y);
    }
}
