using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_SelfTend : PawnColumnWorker_Checkbox
    {
        protected override bool GetValue(Pawn pawn) => pawn.playerSettings.selfTend;

        protected override bool HasCheckbox(Pawn pawn) => pawn.IsColonist && !pawn.Dead && !(pawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Doctor));

        protected override void SetValue(Pawn pawn, bool value)
        {
            if (value && pawn.workSettings.GetPriority(WorkTypeDefOf.Doctor) == 0)
                Messages.Message("MessageSelfTendUnsatisfied".Translate(pawn.LabelShort, pawn), MessageTypeDefOf.CautionInput, false);

            pawn.playerSettings.selfTend = value;
        }
    }
}
