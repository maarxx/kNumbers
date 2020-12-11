namespace Numbers
{
    using RimWorld;
    using UnityEngine;
    using Verse;

    public class PawnColumnWorker_Ability : PawnColumnWorker_Checkbox
    {
        public static float IconPositionVertical = 35f;
        public static float IconPositionHorizontal = 5f;

        protected override bool GetValue(Pawn pawn)
        {
            AbilityDef abilityDef = def.Ext().ability;
            foreach (Ability a in pawn.abilities.abilities)
            {
                if (a.def == abilityDef)
                {
                    return true;
                }
            }
            return false;
        }

        protected override bool HasCheckbox(Pawn pawn) => pawn.HasPsylink;

        protected override void SetValue(Pawn pawn, bool value)
        {
            // this space intentionally left blank
        }

        protected override string GetHeaderTip(PawnTable table) => "SelfTend".Translate() + "\n\n" + "Numbers_ColumnHeader_Tooltip".Translate();
        //protected override string GetHeaderTip(PawnTable table) => def.Ext().ability.GetTooltip();
        //protected override string GetHeaderTip(PawnTable table) => "";

        protected override string GetTip(Pawn pawn)
        {
            if (GetValue(pawn))
            {
                return def.Ext().ability.GetTooltip();
            }
            return "";
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            //base method would put text on the header because these defs have a label.
            //we don't want text on the header, the icons are sufficient.
            //but we need a bunch of crap from that method to make it interactable.
            //so we avoid calling our parent and copy-paste their code here.

            //base.DoHeader(rect, table);
            Rect interactableHeaderRect = GetInteractableHeaderRect(rect, table);
            if (Mouse.IsOver(interactableHeaderRect))
            {
                Widgets.DrawHighlight(interactableHeaderRect);
                string headerTip = GetHeaderTip(table);
                if (!headerTip.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(interactableHeaderRect, headerTip);
                }
            }
            if (Widgets.ButtonInvisible(interactableHeaderRect))
            {
                HeaderClicked(rect, table);
            }

            float scale = 0.5f;
            Texture2D abilityIcon = def.Ext().ability.uiIcon;
            //Texture2D abilityIcon = ContentFinder<Texture2D>.Get(def.Ext().ability.iconPath);
            Vector2 headerIconSize = new Vector2(abilityIcon.width, abilityIcon.height) * scale;
            int     num            = (int)((rect.width - headerIconSize.x) / 2f);
            Rect    position       = new Rect(rect.x + num + IconPositionHorizontal, rect.yMax - abilityIcon.height + IconPositionVertical, headerIconSize.x, headerIconSize.y);
            GUI.DrawTexture(position, abilityIcon);
        }
    }
}
