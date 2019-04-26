using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Numbers.PawnColumnWorkers
{
    using RimWorld;
    using Verse;

    class PawnColumnWorker_Faction : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
            => pawn.Faction?.Name;
    }
}
