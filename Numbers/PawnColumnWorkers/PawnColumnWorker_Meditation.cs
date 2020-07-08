using RimWorld;
using Verse;

namespace Numbers
{
    public class PawnColumnWorker_Meditation : PawnColumnWorker_Text
    {
        protected override string GetTextFor(Pawn pawn)
        {
            return MeditationUtility.FocusTypesAvailableForPawnString(pawn);
        }
    }
}
