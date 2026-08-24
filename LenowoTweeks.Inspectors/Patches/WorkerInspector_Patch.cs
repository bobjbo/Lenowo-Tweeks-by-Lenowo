using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.UIX;

using HarmonyLib;

using LenowoTweeks.Core;

namespace LenowoTweeks.Inspectors.Patches;


[HarmonyPatch]
public class WorkerInspector_Patch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(WorkerInspector), "BuildUIForComponent")]
	public static bool BuildUIForComponent(WorkerInspector __instance, SyncRef<Worker> ____targetWorker, Worker worker, bool allowRemove = true, bool allowDuplicate = true, bool allowContainer = false, Predicate<ISyncMember> memberFilter = null)
	{
		// only run the filtering code on host
		// also only filter if the inspector is not owned by host
		if (__instance.LocalUser.IsHost && !Helpers.ModShouldRun(__instance.Slot.Parent.GetObjectRoot())) return true;

		// possibly prevent error crashing shit
		try
		{
			if (__instance == null || __instance.IsRemoved || __instance.Slot == null || __instance.Slot.IsRemoved || worker == null || worker.IsRemoved) return false;
			UIBuilder ui = new(__instance.Slot);
			if (ui == null) return false;
			RadiantUI_Constants.SetupEditorStyle(ui);
			ui.Style.RequireLockInToPress = true;
			Slot componentVL = ui.VerticalLayout(6f).Slot;
			if (worker is not Slot)
			{
				ui.Style.MinHeight = 32f;
				ui.HorizontalLayout(4f);
				ui.Style.MinHeight = 24f;
				ui.Style.FlexibleWidth = 1000f;
				Button button = ui.Button(GetComponentHeaderName(worker), new colorX?(RadiantUI_Constants.BUTTON_COLOR));
				button.Label.Color.Value = RadiantUI_Constants.LABEL_COLOR;
				if (allowRemove || allowDuplicate || allowContainer)
				{
					ui.Style.FlexibleWidth = 0f;
					ui.Style.MinWidth = 40f;
					if (allowContainer && worker.FindNearestParent<Slot>() != null)
					{
						ButtonRefRelay<Worker> buttonRefRelay = ui.Button(OfficialAssets.Graphics.Icons.Inspector.RootUp, new colorX?(RadiantUI_Constants.Sub.PURPLE)).Slot.AttachComponent<ButtonRefRelay<Worker>>();
						buttonRefRelay.Argument.Target = worker;
						buttonRefRelay.ButtonPressed.Target = __instance.GetMethodDelegate<ButtonEventHandler<Worker>>("OnOpenContainerPressed");
					}
					if (!LenowoTweeks_Inspectors.nohelp.Value)
					{
						Type type = worker.GetType();
						Hyperlink.AttachForWikiPage(ui.Button(OfficialAssets.Graphics.Icons.Inspector.Help, new colorX?(RadiantUI_Constants.Sub.CYAN)).Slot, type);
					}
					if (allowDuplicate)
					{
						ButtonRefRelay<Worker> buttonRefRelay2 = ui.Button(OfficialAssets.Graphics.Icons.Inspector.Duplicate, new colorX?(RadiantUI_Constants.Sub.GREEN)).Slot.AttachComponent<ButtonRefRelay<Worker>>();
						buttonRefRelay2.Argument.Target = worker;
						buttonRefRelay2.ButtonPressed.Target = __instance.GetMethodDelegate<ButtonEventHandler<Worker>>("OnDuplicateComponentPressed");
					}
					if (allowRemove)
					{
						ButtonRefRelay<Worker> buttonRefRelay3 = ui.Button(OfficialAssets.Graphics.Icons.Inspector.Destroy, new colorX?(RadiantUI_Constants.Sub.RED)).Slot.AttachComponent<ButtonRefRelay<Worker>>();
						buttonRefRelay3.Argument.Target = worker;
						buttonRefRelay3.ButtonPressed.Target = __instance.GetMethodDelegate<ButtonEventHandler<Worker>>("OnRemoveComponentPressed");
					}
				}
				button.Slot.AttachComponent<ReferenceProxySource>().Reference.Target = worker;
				ui.NestOut();
				if (____targetWorker.Target == null && LenowoTweeks_Inspectors.collapseComponents.Value)
				{
					Slot content = ui.VerticalLayout(8f).Slot;
					content.Name = "Me when the best collapsable component system :3";
					content.GetComponent<LayoutElement>().Destroy();
					var b = button.Slot.AttachComponent<ButtonToggle>();
					b.TargetValue.Target = content.ActiveSelf_Field;
					content.ActiveSelf = false;
					content.ActiveSelf_Field.OnValueChange += (v) =>
					{
						// also need to do the same trycatch here otherwise it may also error
						try
						{
							if (__instance == null || __instance.IsRemoved || __instance.Slot == null || __instance.Slot.IsRemoved || worker == null || worker.IsRemoved) return;
							if (content.ChildrenCount == 0 && v)
							{
								ui.NestInto(content);
								InspectorHeaderAttribute header = worker.GetType().GetCustomAttribute<InspectorHeaderAttribute>();
								if (header != null)
								{
									AddHeaderText(ui, header);
								}
								if (worker is ICustomInspector customInspector)
								{
									try
									{
										ui.Style.MinHeight = 24f;
										customInspector.BuildInspectorUI(ui);

									} catch (Exception ex)
									{
										LocaleString text = "EXCEPTION BUILDING UI. See log";
										ui.Text(in text);
										UniLog.Error(ex.ToString(), stackTrace: false);
									}
								}
								else
								{
									WorkerInspector.BuildInspectorUI(worker, ui, memberFilter);
								}
								ui.Style.MinHeight = 8f;
								ui.Panel();
								ui.NestOut();
							}

							Helpers.SetConfigVariable(__instance.LocalUser, "CollapsedColor", LenowoTweeks_Inspectors.collapsedComponentColor.Value);
							Helpers.SetConfigVariable(__instance.LocalUser, "ExpandedColor", LenowoTweeks_Inspectors.expandedComponentColor.Value);
						} catch (Exception e)
						{
							UniLog.Error($"LenowoTweeks // You broke it - Failed on ExpandComponent:\n{e}");
						}
					};
					if (LenowoTweeks_Inspectors.copyComponentsToButtons.Value)
					{
						Slot componentHeaderRef = Helpers.GetConfigReference<Slot>(__instance.LocalUser, "UIComponents.Component");
						if (componentHeaderRef != null)
						{
							b.Slot.CopyComponents(componentHeaderRef);
						}
					}
					ui.NestOut();
					Helpers.SetConfigVariable(__instance.LocalUser, "CollapsedColor", LenowoTweeks_Inspectors.collapsedComponentColor.Value);
					Helpers.SetConfigVariable(__instance.LocalUser, "ExpandedColor", LenowoTweeks_Inspectors.expandedComponentColor.Value);
					var bvd = button.Slot.AttachComponent<BooleanValueDriver<colorX>>();
					button.ColorDrivers[1].ColorDrive.Target = null;
					bvd.TargetField.Target = button.Label.Color;
					bvd.TrueValue.Value = RadiantUI_Constants.LABEL_COLOR;
					Helpers.DriveFromVariable(__instance.LocalUser, "ExpandedColor", bvd.TrueValue);
					Helpers.DriveFromVariable(__instance.LocalUser, "CollapsedColor", bvd.FalseValue);
					bvd.State.DriveFrom(content.ActiveSelf_Field);
				}

			}

			if (worker is ICustomInspector customInspector)
			{
				try
				{
					if (worker is Slot || ____targetWorker.Target != null || !LenowoTweeks_Inspectors.collapseComponents.Value)
					{
						ui.Style.MinHeight = 24f;
						customInspector.BuildInspectorUI(ui);
					}

				} catch (Exception ex)
				{
					ui.Text((LocaleString)"EXCEPTION BUILDING UI. See log");
					UniLog.Error(ex.ToString(), stackTrace: false);
				}
			}
			else
			{
				if (worker is Slot || ____targetWorker.Target != null || !LenowoTweeks_Inspectors.collapseComponents.Value)
				{
					WorkerInspector.BuildInspectorUI(worker, ui, memberFilter);
				}
			}

			if (worker is Slot || !LenowoTweeks_Inspectors.collapseComponents.Value)
			{
				ui.Style.MinHeight = 8f;
				ui.Panel();
			}

			ui.NestOut();
		} catch (Exception e)
		{
			UniLog.Error($"LenowoTweeks // You broke it - Failed on BuildUIForComponent:\n{e}");
		}

		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(WorkerInspector), nameof(WorkerInspector.Setup))]
	public static void WorkerInspectorFinder(WorkerInspector __instance)
	{
		if (!LenowoTweeks_Inspectors.variableSpaceForWorkers.Value) return;
		__instance.Slot.AttachComponent<DynamicVariableSpace>();
		DynamicVariableHelper.CreateVariable<bool>(__instance.Slot, "IsWorkerInspector", true, false);
	}

	public static void AddHeaderText(UIBuilder ui, InspectorHeaderAttribute header)
	{
		ui.PushStyle();
		ui.Style.MinHeight = header.MinHeight;
		ui.Text(in header.LocaleKey, bestFit: true, Alignment.TopLeft);
		ui.PopStyle();
	}

	private static string GetComponentHeaderName(Worker worker)
	{
		Type workerType = worker.GetType();
		string headerName = "<b>" + workerType.GetNiceName() + "</b>";
		if (!LenowoTweeks_Inspectors.modifiedComponentHeaders.Value) return headerName;
		if (worker is IDynamicVariable dynVar)
		{
			string dynvarCustomHeader = LenowoTweeks_Inspectors.dynvarComponentHeaderName.Value ?? "";
			string[] split = dynvarCustomHeader.Split(';');
			string varHeader = split.Length >= 1 ? split[0] : "";
			if (string.IsNullOrEmpty(varHeader)) varHeader = "Variable";
			string fieldHeader = split.Length >= 2 ? split[1] : "";
			if (string.IsNullOrEmpty(fieldHeader)) fieldHeader = "Field";
			string referenceHeader = split.Length >= 3 ? split[2] : "";
			if (string.IsNullOrEmpty(referenceHeader)) referenceHeader = "Reference";

			headerName = $"<b>{((workerType == typeof(DynamicTypeVariable) || workerType == typeof(DynamicTypeField)) ? "Type" : workerType.GenericTypeArguments.First().GetNiceName())} Variable: {(string.IsNullOrWhiteSpace(dynVar.VariableName) ? "<i>unset</i>" : dynVar.VariableName)}</b>";
			string niceName = workerType.GetNiceName();
			if (niceName.Contains("Variable") && !niceName.Contains("Variable<") && !niceName.Contains("Type"))
			{
				string variableComponentName = niceName.Substring(niceName.IndexOf("Variable"), MathX.Clamp(niceName.IndexOf('<') - niceName.IndexOf("Variable"), 0, 1000));
				headerName = headerName.Replace("Variable", Regex.Replace(variableComponentName, "(?<!^)([A-Z])", " $1"));
			}
			else if (workerType == typeof(DynamicTypeVariable) || workerType == typeof(DynamicTypeField))
			{
				headerName = headerName.Replace("Variable", workerType == typeof(DynamicTypeField) ? fieldHeader : "Variable");
			}
			else if (workerType.GetGenericTypeDefinition() == typeof(DynamicField<>) || workerType.GetGenericTypeDefinition() == typeof(DynamicReference<>))
			{
				headerName = headerName.Replace("Variable", (workerType.GetGenericTypeDefinition() == typeof(DynamicField<>)) ? fieldHeader : referenceHeader);
			}
			headerName = headerName.Replace("Variable", varHeader);
		}
		if (worker is DynamicVariableSpace space)
		{
			headerName = $"<b>{space.GetType().GetNiceName()}: {(string.IsNullOrWhiteSpace(space.SpaceName) ? "<i>unset</i>" : space.SpaceName)}</b>";
		}
		return headerName;
	}


	[HarmonyTranspiler]
	[HarmonyPatch(typeof(Slot), "BuildInspectorUI")]
	public static IEnumerable<CodeInstruction> SlotUITranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo searchFunc = AccessTools.Method(typeof(UIBuilder), nameof(UIBuilder.NestOut));
		var codes = new List<CodeInstruction>(instructions);
		bool found1 = false;
		bool found2 = false;
		for (int i = 0; i < codes.Count; i++)
		{
			var code = codes[i];
			if (!found1 && code.OperandIs("Inspector.Slot.Reset.Scale")) found1 = true;
			if (found1 && !found2 && code.Calls(searchFunc))
			{
				codes[i] = new(OpCodes.Ldarg_0);
				codes.Insert(i + 1, new(OpCodes.Call, ((Delegate)ResetAllMethod).Method));
				found2 = true;
			}
		}
		return codes.AsEnumerable();
	}

	public static void ResetAllMethod(UIBuilder ui, Slot instance)
	{
		if (LenowoTweeks_Inspectors.slotInspectorResetAll.Value)
		{
			Button allButton = ui.Button("All".AsLocaleKey());

			allButton.Slot.AttachComponent<ButtonRelay>().ButtonPressed.Target = instance.GetMethodDelegate<ButtonEventHandler>("ResetPosition");
			allButton.Slot.AttachComponent<ButtonRelay>().ButtonPressed.Target = instance.GetMethodDelegate<ButtonEventHandler>("ResetRotation");
			allButton.Slot.AttachComponent<ButtonRelay>().ButtonPressed.Target = instance.GetMethodDelegate<ButtonEventHandler>("ResetScale");
		}
		ui.NestOut();
	}
}
