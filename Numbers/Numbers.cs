using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using RimWorld;
using Verse;
using Harmony;
using UnityEngine;

namespace Numbers
{
    using JetBrains.Annotations;

    public class Numbers : Mod
    {
        private readonly Numbers_Settings settings;

        public Numbers(ModContentPack content) : base(content)
        {
            HarmonyInstance harmony = HarmonyInstance.Create("mehni.rimworld.numbers");
            //HarmonyInstance.DEBUG = true;

            harmony.Patch(AccessTools.Method(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve)),
                          null, new HarmonyMethod(typeof(Numbers), nameof(Columndefs)));

            harmony.Patch(AccessTools.Method(typeof(PawnColumnWorker), "HeaderClicked"),
                          new HarmonyMethod(typeof(Numbers), nameof(RightClickToRemoveHeader)));

            harmony.Patch(AccessTools.Method(typeof(PawnTable), nameof(PawnTable.PawnTableOnGUI)),
                          transpiler: new HarmonyMethod(typeof(Numbers), nameof(MakeHeadersReOrderable)));

            harmony.Patch(AccessTools.Method(typeof(PawnColumnWorker), nameof(PawnColumnWorker.DoHeader)),
                          transpiler: new HarmonyMethod(typeof(Numbers), nameof(UseWordWrapOnHeaders)));

            harmony.Patch(AccessTools.Method(typeof(PawnColumnWorker_Text), nameof(PawnColumnWorker_Text.DoCell)),
                          transpiler: new HarmonyMethod(typeof(Numbers), nameof(CentreCell)));

            harmony.Patch(AccessTools.Method(typeof(ReorderableWidget), nameof(ReorderableWidget.Reorderable)),
                          transpiler: new HarmonyMethod(typeof(Numbers), nameof(ReorderWidgetFromEventToInputTranspiler)));

            //we meet again, Fluffy.
            Type pawnColumWorkerType = GenTypes.GetTypeInAnyAssembly("WorkTab.PawnColumnWorker_WorkType");
            if (pawnColumWorkerType != null && typeof(PawnColumnWorker).IsAssignableFrom(pawnColumWorkerType))
            {
                harmony.Patch(AccessTools.Method(pawnColumWorkerType, "HeaderInteractions"),
                        new HarmonyMethod(typeof(Numbers), nameof(RightClickToRemoveHeader)));
            }

            this.settings = this.GetSettings<Numbers_Settings>();
        }

        /// <summary>
        /// TOO MUCH OF A MESS TO EXPLAIN
        /// </summary>
        /// <returns>Madness.</returns>
        private static IEnumerable<CodeInstruction> ReorderWidgetFromEventToInputTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo GetCurrent = AccessTools.Property(typeof(Event), nameof(Event.current)).GetGetMethod();
            MethodInfo GetRawType = AccessTools.Property(typeof(Event), nameof(Event.rawType)).GetGetMethod();
            MethodInfo NoMouseButtonsPressed = AccessTools.Method(typeof(Numbers), nameof(NoMouseButtonsPressed));
            MethodInfo WasClicked = AccessTools.Method(typeof(Numbers), nameof(WasClicked));

            FieldInfo released = AccessTools.Field(typeof(ReorderableWidget), "released");

            bool yieldNext = true;

            List<CodeInstruction> instructionArr = instructions.ToList();
            for (int i = 0; i < instructionArr.ToArray().Length; i++)
            {
                CodeInstruction instruction = instructionArr[i];
                if (instruction.operand != null && instruction.operand == GetCurrent)
                {
                    if (instructionArr[i + 1].operand != null && instructionArr[i + 1].operand == GetRawType)
                    {
                        //L_02bc: Label1
                        //L_02bc: call UnityEngine.Event get_current()
                        //L_02c1: callvirt EventType get_rawType()
                        //L_02c6: ldc.i4.1
                        // =>
                        // call Input.GetMouseButtonUp(1) (or 0)
                        yield return new CodeInstruction(OpCodes.Nop)
                        {
                            labels = new List<Label> { generator.DefineLabel() }
                        };
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = NoMouseButtonsPressed;
                        instructionArr.RemoveAt(i + 1);
                    }
                }
                if (instruction.opcode == OpCodes.Stsfld && instruction.operand == released)
                {
                    yield return instruction;
                    CodeInstruction codeInst = new CodeInstruction(OpCodes.Ldarg_2)
                    {
                        labels = new List<Label> { generator.DefineLabel(), }
                    };
                    codeInst.labels.AddRange(instructionArr[i + 1].labels);
                    yield return codeInst;
                    yield return new CodeInstruction(OpCodes.Call, WasClicked);
                    yieldNext = false;
                }

                if (!yieldNext && instruction.opcode == OpCodes.Ldarg_1)
                    yieldNext = true;

                if (yieldNext)
                    yield return instruction;

                if (instruction.opcode == OpCodes.Call && instruction.operand == AccessTools.Method(typeof(Mouse), nameof(Mouse.IsOver)))
                    yield return new CodeInstruction(OpCodes.And);
            }
        }

        [UsedImplicitly]
        public static bool NoMouseButtonsPressed() => !Input.GetMouseButton(0) && !Input.GetMouseButton(1);

        [UsedImplicitly]
        public static bool WasClicked(bool useRightButton) => useRightButton && Input.GetMouseButtonDown(1) || !useRightButton && Input.GetMouseButtonDown(0);

        //	if (Event.current.type == EventType.MouseDown && ((useRightButton && Event.current.button == 1) || (!useRightButton && Event.current.button == 0)) && Mouse.IsOver(rect))

        //MethodInfo GetButton = AccessTools.Property(typeof(Event), nameof(Event.button)).GetGetMethod();

        //MethodInfo GetMouseButtonUp = AccessTools.Method(typeof(Input), nameof(Input.GetMouseButtonUp));
        //MethodInfo GetMouseButtonDown = AccessTools.Method(typeof(Input), nameof(Input.GetMouseButtonDown));
        //MethodInfo GetMouseButton = AccessTools.Method(typeof(Input), nameof(Input.GetMouseButton));
        //MethodInfo GetType = AccessTools.Property(typeof(Event), nameof(Event.type)).GetGetMethod();

        //L_02d2: call UnityEngine.Event get_current()
        //L_02d7: callvirt EventType get_type()
        //L_02dc: brtrue Label22
        //L_02e1: ldarg.2
        //L_02e2: brfalse Label23
        //L_02e7: call UnityEngine.Event get_current()
        //L_02ec: callvirt Int32 get_button()
        //L_02f1: ldc.i4.1
        //L_02f2: beq Label24
        //L_02f7: Label23
        //L_02f7: ldarg.2
        //L_02f8: brtrue Label25
        //L_02fd: call UnityEngine.Event get_current()
        //L_0302: callvirt Int32 get_button()
        //L_0307: brtrue Label26

        //public static int storedInt = int.MinValue;

        private static IEnumerable<CodeInstruction> MakeHeadersReOrderable(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo recacheIfDirty = AccessTools.Method(typeof(PawnTable), "RecacheIfDirty");
            MethodInfo reorderableGroup = AccessTools.Method(typeof(Numbers), nameof(Numbers.ReorderableGroup));
            MethodInfo reorderableWidget = AccessTools.Method(typeof(Numbers), nameof(Numbers.CallReorderableWidget));

            CodeInstruction[] codeInstructions = instructions.ToArray();

            for (int i = 0; i < codeInstructions.Length; i++)
            {
                CodeInstruction instruction = codeInstructions[i];
                if (i > 2 && codeInstructions[i - 1].operand != null && codeInstructions[i - 1].operand == recacheIfDirty)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, reorderableGroup);
                    yield return new CodeInstruction(OpCodes.Stloc, 7);
                }

                if (instruction.opcode == OpCodes.Ldloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 4)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, 7);
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, reorderableWidget);
                }
                yield return instruction;
            }
        }

        private static int ReorderableGroup(PawnTable pawnTable)
        {
            if (!(pawnTable is PawnTable_NumbersMain numbersPawnTable))
                return int.MinValue;

            return ReorderableWidget.NewGroup(delegate (int from, int to)
            {
                PawnColumnDef pawnColumnDef = numbersPawnTable.PawnTableDef.columns[from];
                numbersPawnTable.PawnTableDef.columns.Insert(to, pawnColumnDef);
                //if it got inserted at a lower number, the index shifted up 1. If not, stick to the old.
                numbersPawnTable.PawnTableDef.columns.RemoveAt(from >= to ? from + 1 : from);
                numbersPawnTable.SetDirty();
                if (Find.WindowStack.currentlyDrawnWindow is MainTabWindow_Numbers numbers)
                    numbers.RefreshAndStoreSessionInWorldComp();
            }, ReorderableDirection.Horizontal);
        }

        private static void CallReorderableWidget(int groupId, Rect rect)
        {
            if (groupId == int.MinValue)
                return;

            if (ReorderableWidget.Reorderable(groupId, rect))
                Widgets.DrawRectFast(rect, Widgets.WindowBGFillColor * new Color(1f, 1f, 1f, 0.5f));
        }

        private static IEnumerable<CodeInstruction> CentreCell(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            MethodInfo anchorSetter = AccessTools.Property(typeof(Text), nameof(Text.Anchor)).GetSetMethod();
            MethodInfo transpilerHelper = AccessTools.Method(typeof(Numbers), nameof(TranspilerHelper));

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldc_I4_3 && instructionList[i + 1].operand == anchorSetter)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_3); //put Table on stack
                    instruction = new CodeInstruction(OpCodes.Call, transpilerHelper);
                }
                yield return instruction;
            }
        }

        //slight issue with job strings. Meh.
        private static TextAnchor TranspilerHelper(PawnTable table) => table is PawnTable_NumbersMain ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;

        private static IEnumerable<CodeInstruction> UseWordWrapOnHeaders(IEnumerable<CodeInstruction> instructions)
        {
            //MethodInfo transpilerHelper = AccessTools.Method(typeof(Numbers), nameof(HeaderUpperAndDownerHelper));
            MethodInfo Truncate = AccessTools.Method(typeof(GenText), nameof(GenText.Truncate));
            MethodInfo WordWrap = AccessTools.Method(typeof(Numbers_Utility), nameof(Numbers_Utility.WordWrapAt));

            //yield return new CodeInstruction(OpCodes.Ldarg_0); //arg 0 == instance
            //yield return new CodeInstruction(OpCodes.Ldarg_1); //arg 1 == rect
            //yield return new CodeInstruction(OpCodes.Ldarg_2);
            //yield return new CodeInstruction(OpCodes.Call, transpilerHelper);
            //yield return new CodeInstruction(OpCodes.Starg, 1);

            var instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {

                if (instructionList[i].opcode == OpCodes.Ldnull && instructionList[i + 1].operand == Truncate)
                {
                    instructionList[i].opcode = OpCodes.Ldarg_2;
                    instructionList[i + 1].operand = WordWrap;
                }
                yield return instructionList[i];
            }
        }

        //currently unused
        private static Rect HeaderUpperAndDownerHelper(PawnColumnWorker pawnColumnWorker, Rect headerRect, PawnTable table)
        {
            if (!(table is PawnTable_NumbersMain numbersTable))
                return headerRect;

            if (!(pawnColumnWorker is PawnColumnWorker_Text))
                return headerRect;

            Vector2 labelSize = new Vector2(pawnColumnWorker.GetMinWidth(table), pawnColumnWorker.GetMinHeaderHeight(table));

            //why yes, this is inspired by RimWorld.PawnColumnWorker_WorkPriority
            float x = headerRect.center.x;
            Rect rect = new Rect(x - labelSize.x / 2f, headerRect.y - 20f, labelSize.x, labelSize.y + 20f);

            if (pawnColumnWorker.def.moveWorkTypeLabelDown)
                rect.y -= 20f;

            return rect;
        }

        private static bool RightClickToRemoveHeader(PawnColumnWorker __instance, Rect headerRect, PawnTable table)
        {
            if (Event.current.shift)
                return true;

            if (!(table is PawnTable_NumbersMain numbersTable))
                return true;

            if (Event.current.button == 1)
            {
                numbersTable.ColumnsListForReading.RemoveAll(x => x == __instance.def);

                if (Find.WindowStack.currentlyDrawnWindow is MainTabWindow_Numbers numbers)
                    numbers.RefreshAndStoreSessionInWorldComp();

                return false;
            }
            return true;
        }

        private static void Columndefs()
        {
            foreach (PawnColumnDef pawnColumnDef in ImpliedPawnColumnDefs())
            {
                DefGenerator.AddImpliedDef(pawnColumnDef);
            }
            //yeah I will set an icon for it because I can. 
            var pcd = DefDatabase<PawnColumnDef>.GetNamedSilentFail("ManhunterOnDamageChance");
            pcd.headerIcon = "UI/Icons/Animal/Predator";
            var pred = DefDatabase<PawnColumnDef>.GetNamedSilentFail("Predator");
            pred.sortable = true;
        }

        private static IEnumerable<PawnColumnDef> ImpliedPawnColumnDefs()
        {
            foreach (RecordDef record in DefDatabase<RecordDef>.AllDefsListForReading)
            {
                PawnColumnDef columnDefRecord = new PawnColumnDef
                {
                    ////can't have . inside defNames, but GetType() returns NameSpace.Class 
                    defName = "Numbers_" + record.GetType().ToString().Replace('.', '_') + "_" + record.defName,
                    workerClass = typeof(PawnColumnWorker_Record),
                    sortable = true,
                    headerTip = record.description,
                    label = record.LabelCap,
                    modContentPack = record.modContentPack,
                    modExtensions = new List<DefModExtension> { new DefModExtension_PawnColumnDefs() }
                };
                columnDefRecord.GetModExtension<DefModExtension_PawnColumnDefs>().record = record;

                yield return columnDefRecord;
            }

            foreach (PawnCapacityDef capacityDef in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
            {
                PawnColumnDef columnDefCapacity = new PawnColumnDef
                {
                    defName = "Numbers_" + capacityDef.GetType().ToString().Replace('.', '_') + "_" + capacityDef.defName,
                    workerClass = typeof(PawnColumnWorker_Capacity),
                    sortable = true,
                    label = capacityDef.LabelCap,
                    modContentPack = capacityDef.modContentPack,
                    modExtensions = new List<DefModExtension> { new DefModExtension_PawnColumnDefs() }
                };
                columnDefCapacity.GetModExtension<DefModExtension_PawnColumnDefs>().capacity = capacityDef;

                yield return columnDefCapacity;
            }

            foreach (NeedDef need in DefDatabase<NeedDef>.AllDefsListForReading)
            {

                PawnColumnDef columnDefNeed = new PawnColumnDef
                {
                    defName = "Numbers_" + need.GetType().ToString().Replace('.', '_') + "_" + need.defName,
                    workerClass = typeof(PawnColumnWorker_Need),
                    sortable = true,
                    headerTip = need.description,
                    label = need.LabelCap,
                    modContentPack = need.modContentPack,
                    modExtensions = new List<DefModExtension> { new DefModExtension_PawnColumnDefs() }
                };
                columnDefNeed.GetModExtension<DefModExtension_PawnColumnDefs>().need = need;

                yield return columnDefNeed;
            }

            foreach (StatDef stat in DefDatabase<StatDef>.AllDefsListForReading
                                                         .Where(x => x.showOnPawns && !x.alwaysHide))
            {
                PawnColumnDef columnDefStat = new PawnColumnDef
                {
                    defName = "Numbers_" + stat.GetType().ToString().Replace('.', '_') + "_" + stat.defName,
                    workerClass = typeof(PawnColumnWorker_Stat),
                    sortable = true,
                    headerTip = stat.description,
                    label = stat.LabelCap,
                    modContentPack = stat.modContentPack,
                    modExtensions = new List<DefModExtension> { new DefModExtension_PawnColumnDefs() }
                };
                columnDefStat.GetModExtension<DefModExtension_PawnColumnDefs>().stat = stat;

                yield return columnDefStat;
            }

            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                PawnColumnDef columnDefSkill = new PawnColumnDef
                {
                    defName = "Numbers_" + skill.GetType().ToString().Replace('.', '_') + "_" + skill.defName,
                    workerClass = typeof(PawnColumnWorker_Skill),
                    sortable = true,
                    headerTip = skill.description,
                    label = skill.LabelCap,
                    modContentPack = skill.modContentPack,
                    modExtensions = new List<DefModExtension> { new DefModExtension_PawnColumnDefs() }
                };
                columnDefSkill.GetModExtension<DefModExtension_PawnColumnDefs>().skill = skill;

                yield return columnDefSkill;
            }
        }

        private static Vector2 scrollPosition;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Numbers_ShowMoreInfoThanVanilla".Translate(), ref Numbers_Settings.showMoreInfoThanVanilla);
            listingStandard.CheckboxLabeled("Numbers_coolerThanTheWildlifeTab".Translate(), ref Numbers_Settings.coolerThanTheWildlifeTab);
            listingStandard.SliderLabeled("Numbers_MaxTableHeight".Translate(), ref Numbers_Settings.maxHeight, Numbers_Settings.maxHeight.ToStringPercent(), 0.3f, 1);
            listingStandard.End();

            float rowHeight = 20f;
            float buttonHeight = 16f;

            float height = RegenPawnTableDefsFromSettings().Count * rowHeight;
            Rect outRect;
            float num = 0f;
            int num2 = 0;

            //erdelf unknowningly to the rescue
            Widgets.BeginScrollView(inRect.BottomPart(0.6f).TopPart(0.8f), ref scrollPosition, outRect = new Rect(inRect.x, inRect.y - rowHeight * 2, inRect.width - 18f, height));
            List<string> list = RegenPawnTableDefsFromSettings();
            for (int i = 0; i < list.Count; i++)
            {
                string current = list[i];

                if (num + rowHeight >= scrollPosition.y && num <= scrollPosition.y + outRect.height)
                {
                    Rect rect = new Rect(0f, num, outRect.width, rowHeight);
                    if (num2 % 2 == 0)
                    {
                        Widgets.DrawAltRect(rect);
                    }
                    GUI.BeginGroup(rect);
                    Rect rect2 = new Rect(rect.width - buttonHeight, (rect.height - buttonHeight) / 2f, buttonHeight, buttonHeight);
                    if (Widgets.ButtonImage(rect2, StaticConstructorOnGameStart.DeleteX, Color.white, GenUI.SubtleMouseoverColor))
                    {
                        this.settings.storedPawnTableDefs.RemoveAt(i);
                    }
                    TooltipHandler.TipRegion(rect2, "delet this");

                    Rect rect5 = new Rect(0, 0, rect.width - rowHeight - 2f, rect.height);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;

                    Widgets.Label(rect5, current);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.EndGroup();
                }
                num += rowHeight;
                num2++;
            }
            Widgets.EndScrollView();
            //writing is done by closing the window.
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            if (Find.World?.GetComponent<WorldComponent_Numbers>() != null)
                Find.World.GetComponent<WorldComponent_Numbers>().NotifySettingsChanged();
        }

        private readonly List<string> cachedList = new List<string>();

        private List<string> RegenPawnTableDefsFromSettings()
        {
            this.cachedList.Clear();

            foreach (string storedPawnTableDef in this.settings.storedPawnTableDefs)
            {
                if (storedPawnTableDef.Split(',')[1] == "Default")
                    this.cachedList.Add(storedPawnTableDef.Split(',')[0].Split('_')[1] + " (" + storedPawnTableDef.Split(',')[1] + ")"); //Numbers_MainTable,Default => MainTable (Default)
                else
                    this.cachedList.Add(storedPawnTableDef.Split(',')[1]);
            }
            return this.cachedList;
        }

        public override string SettingsCategory() => "Numbers";
    }
}
