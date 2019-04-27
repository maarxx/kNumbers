namespace Numbers
{
    public class MainTabWindow_NumbersAnimals : MainTabWindow_Numbers
    {
        public override void PostOpen()
        {
            pawnTableDef = NumbersDefOf.Numbers_Animals;
            UpdateFilter();
            Notify_ResolutionChanged();
            base.PostOpen();
        }
    }
}