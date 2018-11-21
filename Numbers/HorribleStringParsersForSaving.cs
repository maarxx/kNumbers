using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Numbers
{
    public static class HorribleStringParsersForSaving
    {
        public static string TurnPawnTableDefIntoCommaDelimitedString(PawnTableDef table, bool asDefault = false)
        {
            string label = asDefault ? "Default" : table.label;

            return string.Join(",", new string[] { table.defName, label, TurnPawnTableColumnsIntoCommaDelimitedString(table) });
        }

        private static string TurnPawnTableColumnsIntoCommaDelimitedString(PawnTableDef table)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in table.columns)
            {
                stringBuilder.Append(item.defName);
                stringBuilder.Append(',');
            }
            return stringBuilder.ToString();
        }

        public static PawnTableDef TurnCommaDelimitedStringIntoPawnTableDef(string ptd)
        {
            string[] pawnTableDef = ptd.Split(',');

            PawnTableDef reconstructedPCD = DefDatabase<PawnTableDef>.GetNamedSilentFail(pawnTableDef[0]);

            if (reconstructedPCD != null)
            {
                reconstructedPCD.columns.Clear();
                for (int i = 2; i < pawnTableDef.Length; i++)
                {
                    PawnColumnDef pcd = DefDatabase<PawnColumnDef>.GetNamedSilentFail(pawnTableDef[i]);
                    if (pcd != null)
                        reconstructedPCD.columns.Add(pcd);
                }
                return reconstructedPCD;
            }
            return WorldComponent_Numbers.PrimaryFilter.First().Key;
        }
    }
}
