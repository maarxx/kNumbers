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
    using RimWorld.Planet;

    public class Numbers : Mod
    {
        private readonly Numbers_Settings settings;

        public Numbers(ModContentPack content) : base(content)
        {
            HarmonyInstance harmony = HarmonyInstance.Create("mehni.rimworld.numbers");
            //HarmonyInstance.DEBUG = true;

            harmony.Patch(AccessTools.Method(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve)),
                postfix: new HarmonyMethod(typeof(Numbers), nameof(Columndefs)));

            harmony.Patch(AccessTools.Method(typeof(PawnColumnWorker), "HeaderClicked"),
                prefix: new HarmonyMethod(typeof(Numbers), nameof(RightClickToRemoveHeader)));

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
                    prefix: new HarmonyMethod(typeof(Numbers), nameof(RightClickToRemoveHeader)));
            }

            //we meet Uuugggg again too. Credit where it's due:
            //  https://github.com/alextd/RimWorld-EnhancementPack/blob/master/Source/PawnTableHighlightSelected.cs
            harmony.Patch(AccessTools.Method(typeof(PawnColumnWorker_Label), nameof(PawnColumnWorker_Label.DoCell)),
                postfix: new HarmonyMethod(typeof(Numbers), nameof(AddHighlightToLabel_PostFix)),
                transpiler: new HarmonyMethod(typeof(Numbers), nameof(AddHighlightToLabel_Transpiler)));

            this.settings = this.GetSettings<Numbers_Settings>();
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
            pcd.headerAlwaysInteractable = true;
            var pred = DefDatabase<PawnColumnDef>.GetNamedSilentFail("Predator");
            pred.sortable = true;
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

        private static IEnumerable<CodeInstruction> UseWordWrapOnHeaders(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo Truncate = AccessTools.Method(typeof(GenText), nameof(GenText.Truncate));
            MethodInfo WordWrap = AccessTools.Method(typeof(Numbers_Utility), nameof(Numbers_Utility.WordWrapAt));

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
        public static bool NoMouseButtonsPressed()
            => !Input.GetMouseButton(0)
            && !Input.GetMouseButton(1);

        [UsedImplicitly]
        public static bool WasClicked(bool useRightButton)
            => useRightButton && Input.GetMouseButtonDown(1)
            || !useRightButton && Input.GetMouseButtonDown(0);

        private static void AddHighlightToLabel_PostFix(Rect rect, Pawn pawn)
        {
            if (!Numbers_Settings.pawnTableHighlightSelected) return;

            if (Find.Selector.IsSelected(pawn))
                Widgets.DrawHighlightSelected(rect);
        }

        //again, copied from https://github.com/alextd/RimWorld-EnhancementPack/blob/master/Source/PawnTableHighlightSelected.cs
        private static IEnumerable<CodeInstruction> AddHighlightToLabel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo TryJumpAndSelectInfo = AccessTools.Method(typeof(CameraJumper), nameof(CameraJumper.TryJumpAndSelect));
            MethodInfo EscapeCurrentTabInfo = AccessTools.Method(typeof(MainTabsRoot), nameof(MainTabsRoot.EscapeCurrentTab));

            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Call && i.operand == TryJumpAndSelectInfo)
                    i.operand = AccessTools.Method(typeof(Numbers), nameof(ClickPawn));
                if (i.opcode == OpCodes.Callvirt && i.operand == EscapeCurrentTabInfo)
                    i.operand = AccessTools.Method(typeof(Numbers), nameof(SodThisImOut));

                yield return i;
            }
        }

        public static void ClickPawn(GlobalTargetInfo target)
        {
            if (!Numbers_Settings.pawnTableClickSelect)
            {
                CameraJumper.TryJumpAndSelect(target);
                return;
            }

            if (Current.ProgramState != ProgramState.Playing)
                return;

            if (target.Thing is Pawn pawn && pawn.Spawned)
            {
                if (Event.current.shift)
                {
                    if (Find.Selector.IsSelected(pawn))
                        Find.Selector.Deselect(pawn);
                    else
                        Find.Selector.Select(pawn);
                }
                else if (Event.current.alt)
                {
                    Find.MainTabsRoot.EscapeCurrentTab(false);
                    CameraJumper.TryJumpAndSelect(target);
                }
                else
                {
                    if (Find.Selector.IsSelected(pawn))
                        CameraJumper.TryJump(target);
                    if (!Find.Selector.IsSelected(pawn) || Find.Selector.NumSelected > 1 && Event.current.button == 1)
                    {
                        Find.Selector.ClearSelection();
                        Find.Selector.Select(pawn);
                    }
                }
            }
            else //default
            {
                CameraJumper.TryJumpAndSelect(target);
            }
        }

        public static void SodThisImOut(MainTabsRoot o1, bool o2)
        {
            if (!Numbers_Settings.pawnTableClickSelect)
                o1.EscapeCurrentTab(o2);
        }

        private static IEnumerable<PawnColumnDef> ImpliedPawnColumnDefs()
        {
            foreach (RecordDef record in DefDatabase<RecordDef>.AllDefsListForReading)
            {
                PawnColumnDef columnDefRecord = GenerateNewPawnColumnDefFor(record, true);
                columnDefRecord.workerClass = typeof(PawnColumnWorker_Record);
                columnDefRecord.GetModExtension<DefModExtension_PawnColumnDefs>().record = record;

                yield return columnDefRecord;
            }

            foreach (PawnCapacityDef capacityDef in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
            {
                PawnColumnDef columnDefCapacity = GenerateNewPawnColumnDefFor(capacityDef, false);
                columnDefCapacity.workerClass = typeof(PawnColumnWorker_Capacity);
                columnDefCapacity.GetModExtension<DefModExtension_PawnColumnDefs>().capacity = capacityDef;

                yield return columnDefCapacity;
            }

            foreach (NeedDef need in DefDatabase<NeedDef>.AllDefsListForReading)
            {
                PawnColumnDef columnDefNeed = GenerateNewPawnColumnDefFor(need, true);
                columnDefNeed.workerClass = typeof(PawnColumnWorker_Need);
                columnDefNeed.GetModExtension<DefModExtension_PawnColumnDefs>().need = need;

                yield return columnDefNeed;
            }

            foreach (StatDef stat in DefDatabase<StatDef>.AllDefsListForReading
                                                         .Where(x => x.showOnPawns && !x.alwaysHide))
            {
                PawnColumnDef columnDefStat = GenerateNewPawnColumnDefFor(stat, true);
                columnDefStat.workerClass = typeof(PawnColumnWorker_Stat);
                columnDefStat.GetModExtension<DefModExtension_PawnColumnDefs>().stat = stat;

                yield return columnDefStat;
            }

            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                PawnColumnDef columnDefSkill = GenerateNewPawnColumnDefFor(skill, true);
                columnDefSkill.workerClass = typeof(PawnColumnWorker_Skill);
                columnDefSkill.GetModExtension<DefModExtension_PawnColumnDefs>().skill = skill;

                yield return columnDefSkill;
            }
        }

        private static PawnColumnDef GenerateNewPawnColumnDefFor(Def def, bool prependDescription)
        {
            return new PawnColumnDef
            {
                ////can't have . inside defNames, but GetType() returns NameSpace.Class 
                defName = "Numbers_" + def.GetType().ToString().Replace('.', '_') + "_" + def.defName,
                sortable = true,
                headerTip = (prependDescription ? def.description + "\n\n" : "") + "Numbers_ColumnHeader_Tooltip".Translate(),
                generated = true,
                label = def.LabelCap,
                modContentPack = def.modContentPack,
                modExtensions = new List<DefModExtension> { new DefModExtension_PawnColumnDefs() }
            };
        }


        private static Vector2 scrollPosition;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Numbers_showMoreInfoThanVanilla".Translate(), ref Numbers_Settings.showMoreInfoThanVanilla);
            listingStandard.CheckboxLabeled("Numbers_coolerThanTheWildlifeTab".Translate(), ref Numbers_Settings.coolerThanTheWildlifeTab);
            listingStandard.CheckboxLabeled("Numbers_pawnTableClickSelect".Translate(), ref Numbers_Settings.pawnTableClickSelect, "Numbers_pawnTableClickSelect_Desc".Translate());
            listingStandard.CheckboxLabeled("Numbers_pawnTableHighSelected".Translate(), ref Numbers_Settings.pawnTableHighlightSelected, "Numbers_pawnTableHighSelected_Desc".Translate());
            listingStandard.SliderLabeled("Numbers_maxTableHeight".Translate(), ref Numbers_Settings.maxHeight, Numbers_Settings.maxHeight.ToStringPercent(), 0.3f, 1);
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
            Find.World?.GetComponent<WorldComponent_Numbers>()?.NotifySettingsChanged();
        }

        private readonly List<string> cachedList = new List<string>();

        private List<string> RegenPawnTableDefsFromSettings()
        {
            this.cachedList.Clear();

            foreach (string storedPawnTableDef in this.settings.storedPawnTableDefs)
            {
                if (storedPawnTableDef.Split(',')[1] == "Default")
                    this.cachedList.Add(storedPawnTableDef.Split(',')[0].Split('_')[1] + " (" + storedPawnTableDef.Split(',')[1] + ")");
                //Numbers_MainTable,Default => MainTable (Default)
                else
                    this.cachedList.Add(storedPawnTableDef.Split(',')[1]);
            }
            return this.cachedList;
        }

        public override string SettingsCategory() => "Numbers";
    }
}
