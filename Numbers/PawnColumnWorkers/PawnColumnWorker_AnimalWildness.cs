using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_AnimalWildness : PawnColumnWorker_Text
    {
        public override int Compare(Pawn a, Pawn b)
            => (a.AnimalOrWildMan() || b.AnimalOrWildMan())
                ? GetValue(a).CompareTo(GetValue(b))
                : 0;

        protected override string GetTextFor(Pawn pawn)
            => pawn.AnimalOrWildMan()
                ? GetValue(pawn).ToStringPercent()
                : string.Empty;

        private float GetValue(Pawn pawn)
            => pawn.RaceProps.wildness;
    }
}
