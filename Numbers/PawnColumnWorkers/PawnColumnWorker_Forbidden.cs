using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_Forbidden : PawnColumnWorker_Checkbox
    {
        protected override bool GetValue(Pawn pawn) => ((Thing)pawn.ParentHolder).IsForbidden(Faction.OfPlayer);

        protected override void SetValue(Pawn pawn, bool value)
        {
            ((Thing)pawn.ParentHolder).SetForbidden(value);
        }
    }
}
