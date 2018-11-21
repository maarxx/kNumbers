using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_Backstory : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn) => pawn.story.TitleShortCap;

        public override int GetMinWidth(PawnTable table) => 80;

        public override int Compare(Pawn a, Pawn b) => a.story.TitleShortCap[0].CompareTo(b.story.TitleShortCap[0]);
    }
}
