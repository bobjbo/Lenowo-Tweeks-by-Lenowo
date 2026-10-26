using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Elements.Assets;
using Elements.Core;

using FrooxEngine;
using FrooxEngine.UIX;

using HarmonyLib;

using LenowoTweeks.Core;

using ProtoFlux.Core;

namespace LenowoTweeks.Inspectors.Patches;

[Flags]
public enum CommentHeaderMode
{
	ShowComponent = 1,
	ComponentAbove = 2,
	TextAbove = 4,
	ComponentBelow = 8,
	TextBelow = 16,


	Default = ShowComponent,
	ComponentHeader = ShowComponent | ComponentAbove | TextAbove,
	PureHeader = ComponentAbove | TextBelow,

}

[HarmonyPatch]
public class WorkerInspector_Patch
{
	// would very much rather this be a transpiler patch, as then its a little better with updates
	// this would force a lot of this to change though, to account for it.
	// not 100% sure how to go about this, so for now i will not
	[HarmonyPrefix]
	[HarmonyPatch(typeof(WorkerInspector), "BuildUIForComponent")]
	public static bool BuildUIForComponent(WorkerInspector __instance, SyncRef<Worker> ____targetWorker, Worker worker, bool allowRemove = true, bool allowDuplicate = true, bool allowContainer = false, Predicate<ISyncMember> memberFilter = null)
	{
		// only run the filtering code on host
		// also only filter if the inspector is not owned by host
		if (__instance.LocalUser.IsHost && !Helpers.ModShouldRun(__instance.Slot.Parent.GetObjectRoot())) return true;

		try
		{
			if (__instance == null || __instance.IsRemoved || __instance.Slot == null || __instance.Slot.IsRemoved || worker == null || worker.IsRemoved) return false;
			UIBuilder ui = new(__instance.Slot);
			if (ui == null) return false;
			RadiantUI_Constants.SetupEditorStyle(ui);
			ui.Style.RequireLockInToPress = true;
			Slot componentVL = ui.VerticalLayout(6f).Slot;
			var commentMode = LenowoTweeks_Inspectors.commentHeaderMode.Value;
			var isComment = worker is Comment;
			var comment = isComment ? (Comment)worker : null;
			if (LenowoTweeks_Inspectors.sortComponentsByUpdateOrder.Value)
			{
				if (worker is Component c)
				{
					componentVL.OrderOffset = c.UpdateOrder * 10;
					if (isComment && (commentMode.HasFlag(CommentHeaderMode.ComponentAbove) || commentMode.HasFlag(CommentHeaderMode.ComponentBelow)))
					{
						if (commentMode.HasFlag(CommentHeaderMode.ComponentBelow))
							componentVL.OrderOffset += 5;
						else
							componentVL.OrderOffset -= 5;
					}
				}
				else if (worker is Slot)
				{
					componentVL.OrderOffset = long.MinValue;
				}
			}
			if (isComment && (commentMode.HasFlag(CommentHeaderMode.TextAbove) | commentMode.HasFlag(CommentHeaderMode.TextBelow)))
			{
				var headerSlot = ui.VerticalLayout().Slot;
				var headerText = ui.Text(comment.Text.Value);
				ui.NestOut();
				var headerLayout = headerSlot.GetComponent<LayoutElement>();
				headerLayout.MinHeight.Value = 48;
				headerSlot.AttachComponent<ContentSizeFitter>().VerticalFit.Value = SizeFit.PreferredSize;
				headerSlot.OrderOffset = commentMode.HasFlag(CommentHeaderMode.TextBelow) ? long.MaxValue : long.MinValue;
				headerText.HorizontalAutoSize.Value = true;
				headerSlot.Name = "Header";
				comment.Text.OnValueChange += (v) =>
				{
					headerText.Content.Value = comment.Text;
				};
			}
			if (worker is not Slot && (!isComment || commentMode.HasFlag(CommentHeaderMode.ShowComponent) || worker.LocalUser.EditMode))
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
							// try a bit more to prevent random crashes from other users expanding the component
							if (ui == null || content == null || content.IsRemoved) return;
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
					if (__instance.TargetWorker is Slot container)
					{
						if (container.ComponentCount < LenowoTweeks_Inspectors.collapseMinComponents.Value && worker.SyncMemberCount < LenowoTweeks_Inspectors.collapseMinFields.Value)
						{
							content.ActiveSelf_Field.Value = true;
						}
					}
					if (worker is User)
					{
						content.ActiveSelf_Field.Value = true;
					}
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
					button.ColorDrivers.RemoveAt(1);
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
		DynamicVariableHelper.CreateVariable<bool>(__instance.Slot.GetObjectRoot(), "IsWorkerInspector", true, false);
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
		string headerName = workerType.GetNiceName();
		if (!LenowoTweeks_Inspectors.modifiedComponentHeaders.Value) return $"<b>{headerName}</b>";

		// just in case (so it doesnt break all of component generation)
		try
		{
			// not even 100% sure why but it works so
			Type baseType = workerType.BaseType;
			Type baseParentType = baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType;

			var categoryAttribute = workerType.GetCustomAttribute<CategoryAttribute>();
			string workerCategory = categoryAttribute?.Paths.FirstOrDefault();

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

				headerName = $"{((workerType == typeof(DynamicTypeVariable) || workerType == typeof(DynamicTypeField)) ? "Type" : workerType.GenericTypeArguments.First().GetNiceName())} Variable: {(string.IsNullOrWhiteSpace(dynVar.VariableName) ? "<i>unset</i>" : dynVar.VariableName)}";
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
			else if (worker is DynamicVariableSpace space)
			{
				headerName = $"{space.GetType().GetNiceName()}: {(string.IsNullOrWhiteSpace(space.SpaceName) ? "<i>unset</i>" : space.SpaceName)}";
			}
			else if (baseParentType == typeof(DynamicVariableResetBase<>))
			{
				dynamic dynReset = worker;
				string varName = dynReset.VariableName.Value;
				if (string.IsNullOrWhiteSpace(varName)) varName = "<i>unset</i>";
				Type dataType = workerType.GenericTypeArguments[0];
				headerName = $"{dataType.GetNiceName()} Variable Reset: {varName}";
			}

			else if (baseParentType == typeof(ParentData<>))
			{
				dynamic parentData = worker;
				string tag = parentData.Tag.Value;
				if (string.IsNullOrWhiteSpace(tag)) tag = "<i>unset</i>";
				Type dataType = workerType.GenericTypeArguments[0];
				string typeType = typeof(IWorldElement).IsAssignableFrom(dataType) ? "Reference" : "Value";
				// parent value/reference
				headerName = $"{dataType.GetNiceName()} Parent {typeType}: {tag}";
			}
			else if (baseParentType == typeof(ParentDataLink<>))
			{
				// parent value link/reference link
				dynamic parentData = worker;
				string tag = parentData.MatchTag.Value;
				if (string.IsNullOrWhiteSpace(tag)) tag = "<i>unset</i>";
				Type dataType = workerType.GenericTypeArguments[0];
				string typeType = typeof(IWorldElement).IsAssignableFrom(dataType) ? "Reference" : "Value";
				// parent value/reference
				headerName = $"{dataType.GetNiceName()} Parent {typeType} Link: {tag}";
			}

			else if (worker is ISpatialVariable spatialVar)
			{
				Type spatialType = workerType.GenericTypeArguments[0];
				string typeName = workerType.GetNiceName();
				int endOfStart = typeName.IndexOf("Value");
				if (endOfStart == -1) endOfStart = typeName.IndexOf("Reference");
				var split = SplitByUppercase(typeName[..endOfStart]);
				string ShapeType = split[0];
				string ValueType = string.Join(" ", split[1..]);
				string varName = spatialVar.VariableName;
				if (string.IsNullOrWhiteSpace(varName)) varName = "<i>unset</i>";

				headerName = $"{spatialType.GetNiceName()} {ShapeType} {ValueType} Variable: {varName}";
			}
			// this is possibly the worst way to do this (but it works!)
			else if (workerCategory == "Data/Spatial/Samplers" && workerType != typeof(SpatialVariableBlendDriver))
			{
				// this is also just stupid in general
				dynamic spatial2 = worker;
				string varName = spatial2.VariableName.Value;
				if (string.IsNullOrWhiteSpace(varName)) varName = "<i>unset</i>";
				// variable type
				Type? varType = workerType.IsGenericType ? workerType.GenericTypeArguments[0] : null;
				if (varType == null && workerType == typeof(TypeSpatialVariableDriver)) varType = typeof(Type);
				// BooleanSpatialVariableDriver has no generics and as such i must read it a stupid way
				if (varType == null && spatial2.DefaultValue != null) varType = spatial2.DefaultValue.Value.GetType();
				// extract its end type (driver, collector, partial derivative driver)
				string typeName = workerType.GetNiceName();
				int nameEnd = typeName.IndexOf("<");
				int nameStart = typeName.IndexOf("SpatialVariable") + 15;
				var split = SplitByUppercase(nameEnd < 0 ? typeName[nameStart..] : typeName[nameStart..nameEnd]);
				string compType = string.Join(" ", split);
				// extract its start type (numeric, minmax, cloning)
				var split2 = SplitByUppercase(typeName[..(nameStart - 15)]);
				string specialType = string.Join(" ", split2[..(split2.Count() - 1)]);
				if (specialType != "") specialType += " ";
				headerName = $"{varType.GetNiceName()} {specialType}Spatial {compType}: {varName}";
			}

			else if (worker is CloudVariableBase cloudVar)
			{
				// :(
				string typeName = workerType.GetNiceName();
				var split = SplitByUppercase(typeName[..typeName.IndexOf("<")]);
				string varType = split.Last();
				string activeName = split[0] == "Active" ? "Active " : "";
				string varName = cloudVar.Path.Value;
				if (string.IsNullOrWhiteSpace(varName)) varName = "<i>unset</i>";
				headerName = $"{workerType.GenericTypeArguments[0].GetNiceName()} {activeName}Cloud {varType}: {varName}";
			}
		} catch (Exception e)
		{
			UniLog.Error($"LenowoTweeks // Exception in GetComponentHeaderName. Resetting to default.\n{e}");
			// maybe add a way to know if something failed?
			headerName = $" {workerType.GetNiceName()}";
		}

		return $"<b>{headerName}</b>";
	}

	static string[] SplitByUppercase(string input)
	{
		return Regex.Replace(input, "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1").Trim().Split(" ");
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
