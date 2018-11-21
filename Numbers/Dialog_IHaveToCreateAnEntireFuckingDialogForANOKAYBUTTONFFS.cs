using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    public class Dialog_IHaveToCreateAnEntireFuckingDialogForAGODDAMNOKAYBUTTONFFS : Dialog_Rename
    {
        private readonly PawnTableDef pawnTableDef;

        public Dialog_IHaveToCreateAnEntireFuckingDialogForAGODDAMNOKAYBUTTONFFS(ref PawnTableDef pawnTableDef)
        {
            this.pawnTableDef = pawnTableDef;
            this.curName = pawnTableDef.label;
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            AcceptanceReport result = base.NameIsValid(name);
            if (!result.Accepted)
            {
                return result;
            }
            if (!new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9\\-_]*$").IsMatch(name)) //sanitize user input
            {
                return "Should only contain letters, numbers, underscores, or dashes";
            }
            return true;
        }

        protected override void SetName(string name)
        {
            this.pawnTableDef.label = this.curName;

            string pawnTableDeftoSave = HorribleStringParsersForSaving.TurnPawnTableDefIntoCommaDelimitedString(this.pawnTableDef);

            LoadedModManager.GetMod<Numbers>().GetSettings<Numbers_Settings>().StoreNewPawnTableDef(pawnTableDeftoSave);
        }
    }
}
