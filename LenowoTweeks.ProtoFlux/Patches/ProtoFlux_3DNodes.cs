#if !RELEASE
using Elements.Core;

using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Visuals;
using FrooxEngine.UIX;

using HarmonyLib;

using ProtoFlux.Core;

namespace LenowoTweeks.ProtoFlux.Patches;

// need to remake this, leaving it commented out
[HarmonyPatch]
public class ProtoFlux_3DNodes
{
	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(ProtoFluxNodeVisual), "BuildUI")]
	// public static bool MakeNodes3D_Better(UIBuilder ui, ProtoFluxNode node, ProtoFluxNodeVisual __instance,
	// 	FieldDrive<bool> ____labelBg, SyncRef<Image> ____bgImage, SyncRef<Slot> ____inputsRoot, SyncRef<Slot> ____outputsRoot,
	// 	SyncRef<Slot> ____referencesRoot, FieldDrive<bool> ____overviewVisual, FieldDrive<colorX> ____overviewBg, FieldDrive<bool> ____labelText, SyncRef<HoverArea> ____nodeHoverArea
	// 	)
	// {
	// hardcode to not run because literally, it does nothing useful
	// return true;
	// if (!LenowoTweeks.threedprotoflux.Value) return true;
	// if (!Helpers.ModShouldRun(__instance)) return true;

	// try
	// {
	// 	NodeMetadata metadata = NodeMetadataHelper.GetMetadata(node.NodeType);
	// 	ui.LayoutTarget = __instance.Slot;
	// 	ui.VerticalLayout();
	// 	ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
	// 	____bgImage.Target = ui.Image(RadiantUI_Constants.BG_COLOR, zwrite: true);
	// 	ui.IgnoreLayout();
	// 	____nodeHoverArea.Target = ui.Current.AttachComponent<HoverArea>();
	// 	bool flag = !node.SupressHeaderAndFooter;
	// 	string nodeName = __instance.Node.Target.NodeName;
	// 	bool? overrideOverviewMode = node.OverrideOverviewMode;
	// 	Image image = null;
	// 	Slot slot = null;
	// 	if (flag)
	// 	{
	// 		ui.Style.MinHeight = 24f;
	// 		if (overrideOverviewMode != true)
	// 		{
	// 			image = ui.Panel(RadiantUI_Constants.HEADER);
	// 			slot = ui.Text((LocaleString)nodeName).Slot;
	// 			ui.NestOut();
	// 		}
	// 		else
	// 		{
	// 			ui.Empty();
	// 		}
	// 	}

	// 	ui.Style.MinHeight = 32f;
	// 	ui.Style.SupressLayoutElement = true;
	// 	ui.OverlappingLayout(0f, Alignment.TopCenter).ForceExpandHeight.Value = false;
	// 	____inputsRoot.Target = ui.VerticalLayout().Slot;
	// 	____inputsRoot.Target.Name = "Inputs & Operations";
	// 	ui.Style.SupressLayoutElement = false;
	// 	GenerateOperations(__instance, ui, node, metadata);
	// 	GenerateInputs(__instance, ui, node, metadata);
	// 	ui.NestOut();
	// 	ui.Style.SupressLayoutElement = true;
	// 	____outputsRoot.Target = ui.VerticalLayout().Slot;
	// 	____outputsRoot.Target.Name = "Outputs & Impulses";
	// 	ui.Style.SupressLayoutElement = false;
	// 	GenerateImpulses(__instance, ui, node, metadata);
	// 	GenerateOutputs(__instance, ui, node, metadata);
	// 	ui.NestOut();
	// 	ui.Style.SupressLayoutElement = false;
	// 	node.BuildContentUI(__instance, ui);
	// 	if (overrideOverviewMode ?? true)
	// 	{
	// 		ui.Style.SupressLayoutElement = true;
	// 		Image image2 = ui.Panel(RadiantUI_Constants.BG_COLOR);
	// 		image2.Slot.Name = "Overview";
	// 		ui.IgnoreLayout();
	// 		ui.Text((LocaleString)nodeName);
	// 		ui.NestOut();
	// 		image2.RectTransform.AddFixedPadding(flag ? (-24f) : 0f, 16f, 0f, 16f);
	// 		____overviewBg.Target = image2.Tint;
	// 		if (!overrideOverviewMode.HasValue)
	// 		{
	// 			____overviewVisual.Target = image2.Slot.ActiveSelf_Field;
	// 			if (image != null)
	// 			{
	// 				____labelBg.Target = image.EnabledField;
	// 			}

	// 			if (slot != null)
	// 			{
	// 				____labelText.Target = slot.ActiveSelf_Field;
	// 			}
	// 		}
	// 	}

	// 	ui.NestOut();
	// 	ui.Style.SupressLayoutElement = true;
	// 	____referencesRoot.Target = ui.VerticalLayout().Slot;
	// 	____referencesRoot.Target.Name = "References";
	// 	ui.Style.SupressLayoutElement = false;
	// 	GenerateReferences(__instance, ui, node, metadata);
	// 	GenerateGlobalRefs(__instance, ui, node, metadata);
	// 	ui.NestOut();
	// 	if (flag)
	// 	{
	// 		ui.Style.MinHeight = 16f;
	// 		string workerCategoryPath = node.WorkerCategoryPath;
	// 		if (workerCategoryPath != null)
	// 		{
	// 			ui.Text((LocaleString)Path.GetFileName(workerCategoryPath)).Color.Value = colorX.DarkGray;
	// 		}
	// 	}


	// } catch (Exception e)
	// {
	// 	UniLog.Error("Encountered an ERROR in MakeNodes3D: " + e.Message + "\n" + e.StackTrace, false);
	// }


	// return false;
	// }

	private static void GenerateInputs(ProtoFluxNodeVisual visual, UIBuilder ui, ProtoFluxNode node, NodeMetadata metadata)
	{
		for (int i = 0; i < metadata.FixedInputCount; i++)
		{
			InputMetadata inputMetadata = metadata.FixedInputs[i];
			if (node.GenerateElement(inputMetadata.Name))
			{
				GenerateInputElement(visual, ui, node.GetInput(i), inputMetadata.Name, inputMetadata.InputType);
			}
		}

		for (int j = 0; j < metadata.DynamicInputCount; j++)
		{
			InputListMetadata inputList = metadata.DynamicInputs[j];
			if (node.GenerateElement(inputList.Name))
			{
				GenerateDynamicElement(visual, ui, node, node.GetInputList(j), inputList.Name, isOutput: false, delegate (ProtoFluxInputListManager m)
				{
					m.InputType.Value = inputList.TypeConstraint;
				});
			}
		}
	}

	private static void GenerateOutputs(ProtoFluxNodeVisual visual, UIBuilder ui, ProtoFluxNode node, NodeMetadata metadata)
	{
		for (int i = 0; i < metadata.FixedOutputCount; i++)
		{
			OutputMetadata outputMetadata = metadata.FixedOutputs[i];
			if (node.GenerateElement(outputMetadata.Name))
			{
				GenerateOutputElement(visual, ui, node.GetOutput(i), outputMetadata.Name, outputMetadata.OutputType);
			}
		}

		for (int j = 0; j < metadata.DynamicOutputCount; j++)
		{
			OutputListMetadata outputList = metadata.DynamicOutputs[j];
			if (node.GenerateElement(outputList.Name))
			{
				GenerateDynamicElement(visual, ui, node, node.GetOutputList(j), outputList.Name, isOutput: true, delegate (ProtoFluxOutputListManager m)
				{
					m.OutputType.Value = outputList.TypeConstraint;
				});
			}
		}
	}

	private static void GenerateOperations(ProtoFluxNodeVisual visual, UIBuilder ui, ProtoFluxNode node, NodeMetadata metadata)
	{
		for (int i = 0; i < metadata.FixedOperationCount; i++)
		{
			OperationMetadata operationMetadata = metadata.FixedOperations[i];
			if (node.GenerateElement(operationMetadata.Name))
			{
				GenerateOperationElement(visual, ui, node.GetOperation(i), operationMetadata.Name, operationMetadata.IsAsync);
			}
		}

		for (int j = 0; j < metadata.DynamicOperationCount; j++)
		{
			OperationListMetadata operationList = metadata.DynamicOperations[j];
			GenerateDynamicElement(visual, ui, node, node.GetOperationList(j), operationList.Name, isOutput: false, delegate (ProtoFluxOperationListManager m)
			{
				m.SupportsAsync.Value = operationList.SupportsAsync;
			});
		}
	}

	private static void GenerateImpulses(ProtoFluxNodeVisual visual, UIBuilder ui, ProtoFluxNode node, NodeMetadata metadata)
	{
		for (int i = 0; i < metadata.FixedImpulseCount; i++)
		{
			ImpulseMetadata impulseMetadata = metadata.FixedImpulses[i];
			if (node.GenerateElement(impulseMetadata.Name))
			{
				GenerateImpulseElement(visual, ui, node.GetImpulse(i), impulseMetadata.Name, impulseMetadata.Type);
			}
		}

		for (int j = 0; j < metadata.DynamicImpulseCount; j++)
		{
			ImpulseListMetadata impulseList = metadata.DynamicImpulses[j];
			if (node.GenerateElement(impulseList.Name))
			{
				GenerateDynamicElement(visual, ui, node, node.GetImpulseList(j), impulseList.Name, isOutput: true, delegate (ProtoFluxImpulseListManager m)
				{
					m.ImpulseType.Value = impulseList.Type;
				});
			}
		}
	}

	private static void GenerateReferences(ProtoFluxNodeVisual visual, UIBuilder ui, ProtoFluxNode node, NodeMetadata metadata)
	{
		for (int i = 0; i < metadata.FixedReferenceCount; i++)
		{
			ReferenceMetadata referenceMetadata = metadata.FixedReferences[i];
			if (node.GenerateElement(referenceMetadata.Name))
			{
				GenerateReferenceElement(visual, ui, node.GetReference(i), referenceMetadata.Name, referenceMetadata.ReferenceType);
			}
		}
	}

	private static void GenerateGlobalRefs(ProtoFluxNodeVisual visual, UIBuilder ui, ProtoFluxNode node, NodeMetadata metadata)
	{
		for (int i = 0; i < metadata.FixedGlobalRefCount; i++)
		{
			GlobalRefMetadata globalRefMetadata = metadata.FixedGlobalRefs[i];
			if (node.GenerateElement(globalRefMetadata.Name))
			{
				GenerateGlobalRefElement(visual, ui, globalRefMetadata.Name, globalRefMetadata.ValueType.GetTypeColor().MulRGB(1.5f), globalRefMetadata.ValueType, node.GetGlobalRef(i));
			}
		}
	}

	internal static Slot GenerateInputElement(ProtoFluxNodeVisual visual, UIBuilder ui, ISyncRef input, string name, Type elementType, int? listIndex = null)
	{
		var (result, protoFluxInputProxy) = GenerateFixedElement<ProtoFluxInputProxy>(visual, ui, name, elementType.GetTypeColor().MulRGB(1.5f), elementType.GetTypeConnectorSprite(visual.World), isOutput: false, flipSprite: false, listIndex);
		protoFluxInputProxy.NodeInput.Target = input;
		protoFluxInputProxy.InputType.Value = elementType;
		return result;
	}

	internal static Slot GenerateOutputElement(ProtoFluxNodeVisual visual, UIBuilder ui, INodeOutput output, string name, Type elementType, int? listIndex = null)
	{
		var (result, protoFluxOutputProxy) = GenerateFixedElement<ProtoFluxOutputProxy>(visual, ui, name, elementType.GetTypeColor().MulRGB(1.5f), elementType.GetTypeConnectorSprite(visual.World), isOutput: true, flipSprite: true, listIndex);
		protoFluxOutputProxy.NodeOutput.Target = output;
		protoFluxOutputProxy.OutputType.Value = elementType;
		return result;
	}

	internal static Slot GenerateImpulseElement(ProtoFluxNodeVisual visual, UIBuilder ui, ISyncRef input, string name, ImpulseType type, int? listIndex = null)
	{
		var (result, protoFluxImpulseProxy) = GenerateFixedElement<ProtoFluxImpulseProxy>(visual, ui, name, type.GetImpulseColor().MulRGB(1.5f), visual.World.GetFlowConnectorSprite(), isOutput: true, flipSprite: false, listIndex);
		protoFluxImpulseProxy.NodeImpulse.Target = input;
		protoFluxImpulseProxy.ImpulseType.Value = type;
		return result;
	}

	internal static Slot GenerateOperationElement(ProtoFluxNodeVisual visual, UIBuilder ui, INodeOperation operation, string name, bool isAsync, int? listIndex = null)
	{
		var (result, protoFluxOperationProxy) = GenerateFixedElement<ProtoFluxOperationProxy>(visual, ui, name, DatatypeColorHelper.GetOperationColor(isAsync).MulRGB(1.5f), visual.World.GetFlowConnectorSprite(), isOutput: false, flipSprite: false, listIndex);
		protoFluxOperationProxy.NodeOperation.Target = operation;
		protoFluxOperationProxy.IsAsync.Value = isAsync;
		return result;
	}

	internal static void GenerateReferenceElement(ProtoFluxNodeVisual visual, UIBuilder ui, ISyncRef reference, string name, Type referenceType, int? listIndex = null)
	{
		GenerateRefElement(visual, ui, name, referenceType.GetTypeColor().MulRGB(1.5f), delegate (ProtoFluxReferenceProxy proxy, Text label, Slot connectPoint)
		{
			proxy.Node.Target = visual.Node.Target;
			proxy.NodeReference.Target = reference;
			proxy.ValueType.Value = referenceType;
			proxy.ConnectPoint.Target = connectPoint;
			ProxyBuildUI(proxy, label, ui);
		});
	}

	[HarmonyReversePatch]
	[HarmonyPatch(typeof(ProtoFluxReferenceProxy), "BuildUI")]
	private static void ProxyBuildUI(ProtoFluxReferenceProxy instance, Text label, UIBuilder ui) => throw new NotImplementedException("cannot build proxy ui!");

	internal static (Slot slot, P proxy) GenerateFixedElement<P>(ProtoFluxNodeVisual visual, UIBuilder ui, string name, in colorX color, IAssetProvider<Sprite> sprite, bool isOutput, bool flipSprite, int? listIndex = null) where P : ProtoFluxElementProxy, new()
	{
		RectTransform rectTransform = ui.Panel();
		rectTransform.Slot.Name = name ?? listIndex?.ToString() ?? "Element";
		Image image = ui.Image(sprite, in color);
		image.Material.Target = visual.World.GetDefaultOpaqueDualsidedUI_Unlit();
		image.Slot.Name = "Connector";
		if (flipSprite)
		{
			image.FlipHorizontally.Value = true;
		}

		if (isOutput)
		{
			image.RectTransform.SetFixedHorizontal(-16f, 0f, 1f);
		}
		else
		{
			image.RectTransform.SetFixedHorizontal(0f, 16f, 0f);
		}

		Slot slot = image.Slot.AddSlot("<WIRE_POINT>");
		RectTransform rectTransform2 = slot.AttachComponent<RectTransform>();
		rectTransform2.AnchorMin.Value = new float2(isOutput ? 1f : 0f, 0.5f);
		rectTransform2.AnchorMax.Value = new float2(isOutput ? 1f : 0f, 0.5f);
		slot.AttachComponent<RectSlotDriver>();
		P val = image.Slot.AttachComponent<P>();
		ProtoFluxNode target = visual.Node.Target;
		val.Node.Target = target;
		val.ElementName.Value = name;
		val.IsDynamic.Value = listIndex.HasValue;
		val.Index.Value = listIndex.GetValueOrDefault();
		val.ConnectPoint.Target = rectTransform2.Slot;
		if (!listIndex.HasValue && !target.SupressLabels)
		{
			Image image2 = ui.Panel(color.SetA(0.3f));
			if (isOutput)
			{
				image2.RectTransform.AnchorMax.Value = new float2(1f, 0.5f);
				image2.RectTransform.OffsetMin.Value = new float2(32f);
				image2.RectTransform.OffsetMax.Value = new float2(-16f);
			}
			else
			{
				image2.RectTransform.AnchorMin.Value = new float2(0f, 0.5f);
				image2.RectTransform.OffsetMin.Value = new float2(16f);
				image2.RectTransform.OffsetMax.Value = new float2(-32f);
			}

			image2.RectTransform.AddFixedVerticalPadding(2f);
			ui.Text((LocaleString)name, bestFit: true, isOutput ? Alignment.BottomRight : Alignment.TopLeft).RectTransform.AddFixedPadding(2f);
			ui.NestOut();
		}

		ui.NestOut();
		return (slot: rectTransform.Slot, proxy: val);
	}

	internal static void GenerateRefElement<P>(ProtoFluxNodeVisual visual, UIBuilder ui, string name, in colorX color, Action<P, Text, Slot> proxySetup) where P : ProtoFluxRefProxy, new()
	{
		ui.Panel();
		ui.VerticalHeader(4f, out RectTransform header, out RectTransform content);
		ui.ForceNext = header;
		Image image = ui.Image(in color);
		image.Material.Target = visual.World.GetDefaultOpaqueDualsidedUI_Unlit();
		Slot slot = image.Slot.AddSlot("<WIRE_POINT>");
		RectTransform rectTransform = slot.AttachComponent<RectTransform>();
		rectTransform.AnchorMin.Value = new float2(0f, 0.5f);
		rectTransform.AnchorMax.Value = new float2(0f, 0.5f);
		slot.AttachComponent<RectSlotDriver>();
		ui.NestInto(content);
		ui.SplitVertically(0.5f, out RectTransform top, out RectTransform bottom);
		ui.ForceNext = top;
		Text arg = ui.Text((LocaleString)name);
		ui.NestInto(bottom);
		P arg2 = ui.Root.AttachComponent<P>();
		proxySetup(arg2, arg, slot);
		ui.NestOut();
		ui.NestOut();
		ui.NestOut();
	}

	internal static void GenerateGlobalRefElement(ProtoFluxNodeVisual visual, UIBuilder ui, string name, in colorX color, Type referenceType, ISyncRef globalRef)
	{
		GenerateRefElement(visual, ui, name, in color, delegate (ProtoFluxGlobalRefProxy refProxy, Text label, Slot connectPoint)
		{
			refProxy.Node.Target = visual.Node.Target;
			refProxy.ValueType.Value = referenceType;
			GlobalProxyBuildUI(refProxy, label, ui, globalRef);
		});
	}


	[HarmonyReversePatch]
	[HarmonyPatch(typeof(ProtoFluxGlobalRefProxy), "BuildUI")]
	private static void GlobalProxyBuildUI(ProtoFluxGlobalRefProxy instance, Text label, UIBuilder ui, ISyncRef globalRef) => throw new NotImplementedException("cannot build proxy ui!");

	private static T GenerateDynamicElement<T>(ProtoFluxNodeVisual visual, UIBuilder ui, ProtoFluxNode node, ISyncList list, string name, bool isOutput, Action<T> postprocess) where T : ProtoFluxDynamicElementManager, new()
	{
		ui.Style.SupressLayoutElement = true;
		VerticalLayout verticalLayout = ui.VerticalLayout(0f, 0f, Alignment.TopLeft);
		verticalLayout.ForceExpandHeight.Value = false;
		verticalLayout.Slot.Name = name;
		LayoutElement layoutElement = ui.Root.AttachComponent<LayoutElement>();
		layoutElement.MinHeight.Value = 64f;
		layoutElement.Priority.Value = 0;
		T val = ui.Root.AttachComponent<T>();
		val.Visual.Target = visual;
		val.List.Target = list;
		postprocess(val);
		if (!node.SupressLabels)
		{
			Text text = ui.Text((LocaleString)name, bestFit: true, isOutput ? Alignment.BottomRight : Alignment.TopLeft);
			ui.IgnoreLayout();
			text.Slot.OrderOffset = 32767L;
			if (isOutput)
			{
				text.RectTransform.OffsetMin.Value = new float2(16f, -32f);
				text.RectTransform.OffsetMax.Value = new float2(-16f, -16f);
			}
			else
			{
				text.RectTransform.OffsetMin.Value = new float2(16f, -16f);
				text.RectTransform.OffsetMax.Value = new float2(-16f);
			}

			text.RectTransform.AnchorMin.Value = new float2(0f, 1f);
			text.RectTransform.AnchorMax.Value = new float2(1f, 1f);
			text.RectTransform.AddFixedPadding(1f);
		}

		Image image = ui.Image(colorX.White.SetA(0.25f));
		ui.IgnoreLayout();
		image.Slot.OrderOffset = 32768L;
		if (isOutput)
		{
			image.RectTransform.OffsetMin.Value = new float2(-30.5f, 19.2f);
			image.RectTransform.OffsetMax.Value = new float2(-27.5f, -19.2f);
			image.RectTransform.AnchorMin.Value = new float2(1f);
			image.RectTransform.AnchorMax.Value = new float2(1f, 1f);
		}
		else
		{
			image.RectTransform.OffsetMin.Value = new float2(30.5f, 19.2f);
			image.RectTransform.OffsetMax.Value = new float2(33.5f, -19.2f);
			image.RectTransform.AnchorMin.Value = new float2(0f, 0f);
			image.RectTransform.AnchorMax.Value = new float2(0f, 1f);
		}

		RectTransform rectTransform = ui.Panel();
		ui.IgnoreLayout();
		rectTransform.Slot.OrderOffset = 32769L;
		rectTransform.OffsetMin.Value = new float2(16f);
		rectTransform.OffsetMax.Value = new float2(-16f, 16f);
		if (isOutput)
		{
			rectTransform.AnchorMin.Value = new float2(0.5f);
			rectTransform.AnchorMax.Value = new float2(1f);
		}
		else
		{
			rectTransform.AnchorMin.Value = new float2(0f, 0f);
			rectTransform.AnchorMax.Value = new float2(0.5f);
		}

		ui.HorizontalLayout(2f);
		ui.Style.SupressLayoutElement = false;
		Button button = ui.Button((LocaleString)"+", val.AddElement);
		Button button2 = ui.Button((LocaleString)"-", val.RemoveElement);
		val.AddButtonEnabled.Target = button.EnabledField;
		val.RemoveButtonEnabled.Target = button2.EnabledField;
		GenerateList(val, visual, ui, list);
		ui.NestOut();
		ui.NestOut();
		ui.NestOut();
		return val;
	}


	internal static void GenerateList(ProtoFluxDynamicElementManager instance, ProtoFluxNodeVisual visual, UIBuilder ui, ISyncList list)
	{
		ui.Style.MinHeight = 32f;
		ui.NestInto(instance.Slot);
		var traverse = Traverse.Create(instance);
		var elements = traverse.Field<SyncRefList<Slot>>("_elements").Value;
		for (int i = 0; i < list.Count; i++)
		{
			Slot slot;
			if (elements.Count <= i)
			{
				slot = traverse.Method("GenerateElement", [visual, ui, i]).GetValue<Slot>();
				elements.Add(slot);
			}
			else
			{
				slot = elements[i];
			}

			slot.OrderOffset = i;
		}

		while (elements.Count > list.Count)
		{
			elements[elements.Count - 1].Destroy();
			elements.RemoveAt(elements.Count - 1);
		}

		ui.NestOut();
		instance.AddButtonEnabled.Target.Value = true;
		instance.RemoveButtonEnabled.Target.Value = list.Count > instance.MinElements.Value;
	}

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(ProtoFluxNodeVisual), "BuildUI")]
	// public static void MakeNodes3D(UIBuilder ui, ProtoFluxNodeVisual __instance, FieldDrive<bool> ____labelBg)
	// {
	// 	if (!LenowoTweeks.threedprotoflux.Value) return;
	// 	if (!Helpers.ModShouldRun(__instance)) return;
	// 	__instance.StartTask(async () =>
	// 	{

	// 		try
	// 		{
	// 			await new Updates(3);
	// 			// part 1
	// 			Slot uiRoot = __instance.Slot;
	// 			if (!Helpers.ModShouldRun(uiRoot)) return;

	// 			SetupNode(uiRoot, ____labelBg, out bool hasTitleBar, out Slot overlapping, out Slot possibleOverview, out ValueMultiDriver<bool> invertedIsOverviewMulti, out IField<string> nodeTitleField);

	// 			// part 2
	// 			Slot newUI = SetupBackground(uiRoot, hasTitleBar);

	// 			SetupTitle(newUI, uiRoot, nodeTitleField, hasTitleBar, possibleOverview);

	// 			SetupInputs(newUI, overlapping);

	// 			SetupConnectors(__instance, overlapping, invertedIsOverviewMulti);


	// 		} catch (Exception e)
	// 		{
	// 			UniLog.Error("Encountered an ERROR in MakeNodes3D: " + e.Message + "\n" + e.StackTrace, false);
	// 		}

	// 	});


	// }

	// public static void SetupNode(Slot uiRoot, FieldDrive<bool> labelBG, out bool hasTitleBar, out Slot overlapping, out Slot possibleOverview, out ValueMultiDriver<bool> invertedIsOverviewMulti, out IField<string> nodeTitleField)
	// {
	// 	Slot possibleTitle = uiRoot.Children.ToList()[1];
	// 	hasTitleBar = possibleTitle.Name == "Image";
	// 	if (hasTitleBar)
	// 	{
	// 		var titleImage = possibleTitle.GetComponent<Image>();
	// 		if (!titleImage.EnabledField.IsDriven) titleImage.Enabled = false;
	// 		else
	// 		{
	// 			var labelActiveVar = uiRoot.AttachComponent<ValueField<bool>>();
	// 			labelBG.Target = labelActiveVar.Value;
	// 			titleImage.Destroy();
	// 		}
	// 	}
	// 	Slot titleBarSlot = hasTitleBar ? possibleTitle.FindChild("Text") : null;
	// 	overlapping = uiRoot.FindChild("Overlapping Layout");
	// 	possibleOverview = overlapping?.FindChild("Overview")?.Children.First();
	// 	Slot titleOrOverview = hasTitleBar ? titleBarSlot : possibleOverview;
	// 	nodeTitleField = titleOrOverview.GetComponent<Text>().Content;
	// 	var varSpace = uiRoot.AttachComponent<DynamicVariableSpace>();
	// 	varSpace.SpaceName.Value = "3DNode";

	// 	var invertedIsOverview = uiRoot.AttachComponent<BooleanValueDriver<bool>>();
	// 	invertedIsOverview.FalseValue.Value = true;

	// 	invertedIsOverview.State.Value = hasTitleBar;
	// 	if (possibleOverview != null) invertedIsOverview.State.DriveFrom(possibleOverview.Parent.ActiveSelf_Field);

	// 	invertedIsOverviewMulti = uiRoot.AttachComponent<ValueMultiDriver<bool>>();
	// 	invertedIsOverview.TargetField.Target = invertedIsOverviewMulti.Value;

	// 	// this is some fucking bullshit
	// 	uiRoot.Children.First().AttachComponent<Mask>();
	// }

	// public static Slot SetupBackground(Slot uiRoot, bool hasTitleBar)
	// {
	// 	Slot newUI = uiRoot.AddSlot("Bevel BG");
	// 	var uiBaseMesh = newUI.AttachComponent<BevelBoxMesh>();
	// 	uiBaseMesh.Bevel.Value = 2.5f;
	// 	var uiBoundingDriver = newUI.AttachComponent<BoundingBoxDriver>();
	// 	var uiMaterial = newUI.AttachComponent<PBS_Metallic>();
	// 	uiMaterial.AlbedoColor.Value = new(0.22f);
	// 	var uiRenderer = newUI.AttachComponent<MeshRenderer>();
	// 	uiRenderer.Mesh.Target = uiBaseMesh;
	// 	uiRenderer.Material.Target = uiMaterial;
	// 	var rootCollider = uiRoot.GetComponent<BoxCollider>();
	// 	uiBoundingDriver.BoundedSource.Target = rootCollider;
	// 	uiBoundingDriver.Padding.Value = new(0, 0, 5f);
	// 	uiBoundingDriver.Size.Target = uiBaseMesh.Size;
	// 	uiBoundingDriver.Center.Target = newUI.Position_Field;

	// 	var uiBGSizeVar = newUI.AttachComponent<DynamicField<float3>>();
	// 	uiBGSizeVar.VariableName.Value = "3DNode/BGSize";
	// 	uiBGSizeVar.TargetField.Target = uiBaseMesh.Size;
	// 	var uiHasHeaderVar = newUI.AttachComponent<DynamicValueVariable<bool>>();
	// 	uiHasHeaderVar.VariableName.Value = "3DNode/HasHeader";
	// 	uiHasHeaderVar.Value.Value = hasTitleBar;

	// 	return newUI;
	// }

	// public static void SetupTitle(Slot newUI, Slot uiRoot, IField<string> nodeTitleField, bool hasTitleBar, Slot possibleOverview)
	// {
	// 	Slot uiTitleText = newUI.AddSlot("Text");
	// 	var uiTitleRenderer = uiTitleText.AttachComponent<TextRenderer>();
	// 	uiTitleRenderer.Text.DriveFrom(nodeTitleField);
	// 	var uiTitleBoolDriver = uiTitleText.AttachComponent<BooleanValueDriver<float2>>();
	// 	uiTitleBoolDriver.TrueValue.Value = new(0.2f, 0.15f);
	// 	uiTitleBoolDriver.FalseValue.Value = new(0.3f, 0.05f);
	// 	uiTitleBoolDriver.TargetField.Target = uiTitleRenderer.BoundsSize;
	// 	uiTitleBoolDriver.State.Value = !hasTitleBar;
	// 	if (possibleOverview != null) uiTitleBoolDriver.State.DriveFrom(possibleOverview.Parent.ActiveSelf_Field);
	// 	uiTitleRenderer.Bounded.Value = true;
	// 	uiTitleRenderer.HorizontalAutoSize.Value = true;
	// 	uiTitleRenderer.VerticalAutoSize.Value = true;
	// 	var uiTitleAlignDriver = uiTitleText.AttachComponent<BooleanValueDriver<Alignment>>();
	// 	uiTitleAlignDriver.TrueValue.Value = Alignment.MiddleCenter;
	// 	uiTitleAlignDriver.FalseValue.Value = Alignment.TopCenter;
	// 	uiTitleAlignDriver.TargetField.Target = uiTitleRenderer.BoundsAlignment;
	// 	uiTitleAlignDriver.State.Value = !hasTitleBar;
	// 	if (possibleOverview != null) uiTitleAlignDriver.State.DriveFrom(possibleOverview.Parent.ActiveSelf_Field);
	// 	var uiTitlePosDriver = uiTitleText.AttachComponent<BooleanValueDriver<float3>>();
	// 	uiTitlePosDriver.TrueValue.Value = new(0, 0, -3);
	// 	uiTitlePosDriver.TargetField.Target = uiTitleText.Position_Field;
	// 	uiTitlePosDriver.State.Value = !hasTitleBar;
	// 	if (possibleOverview != null) uiTitlePosDriver.State.DriveFrom(possibleOverview.Parent.ActiveSelf_Field);
	// 	uiTitleText.LocalPosition = new(0, uiRoot.ComputeBoundingBox(false, newUI).max.y, -3f);
	// 	uiTitleText.LocalScale = new(400f, 400f, 400f);
	// 	// it gets worse
	// 	Slot uiTitleFlux = uiTitleText.AddSlot("flux flux flux flux flux");
	// 	var titleDrive = uiTitleFlux.AttachComponent<ValueFieldDrive<float3>>();
	// 	var titleDriveProxy = titleDrive.GetRootProxy(true);
	// 	titleDriveProxy.Drive.Target = uiTitlePosDriver.FalseValue;
	// 	var add = uiTitleFlux.AttachComponent<ValueAdd<float3>>();
	// 	var mul = uiTitleFlux.AttachComponent<ValueMul<float3>>();
	// 	var const0 = uiTitleFlux.AttachComponent<ValueInput<float3>>();
	// 	var const1 = uiTitleFlux.AttachComponent<ValueInput<float3>>();
	// 	var const2 = uiTitleFlux.AttachComponent<ValueInput<float3>>();
	// 	var cond0 = uiTitleFlux.AttachComponent<ValueConditional<float3>>();
	// 	var dynIn0 = uiTitleFlux.AttachComponent<DynamicVariableValueInput<float3>>();
	// 	var dynIn1 = uiTitleFlux.AttachComponent<DynamicVariableValueInput<bool>>();
	// 	var dynIn0Target = uiTitleFlux.AttachComponent<GlobalValue<string>>();
	// 	var dynIn1Target = uiTitleFlux.AttachComponent<GlobalValue<string>>();
	// 	var dynIn0Proxy = uiTitleFlux.AttachComponent<ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables.DynamicVariableInputProxy<float3>>();
	// 	var dynIn1Proxy = uiTitleFlux.AttachComponent<ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables.DynamicVariableInputProxy<bool>>();

	// 	const0.Value.Value = new(0, 0, -3);
	// 	const1.Value.Value = new(0, 0.5f, 0);
	// 	// const1.Value.Value = new(0, 0, 0);
	// 	const2.Value.Value = new(0, 1.95f, 0);
	// 	// const2.Value.Value = new(0, 0.475f, 0);
	// 	// const2.Value.Value = new(0, 0, 0);

	// 	dynIn0.VariableName.Target = dynIn0Target;
	// 	dynIn1.VariableName.Target = dynIn1Target;

	// 	dynIn0Target.Value.Value = "3DNode/BGSize";
	// 	dynIn1Target.Value.Value = "3DNode/HasHeader";

	// 	dynIn0Proxy.Node.Target = dynIn0;
	// 	dynIn1Proxy.Node.Target = dynIn1;

	// 	cond0.OnTrue.Target = const1;
	// 	cond0.OnFalse.Target = const2;
	// 	cond0.Condition.Target = dynIn1.Value;

	// 	mul.A.Target = dynIn0.Value;
	// 	mul.B.Target = cond0;

	// 	add.A.Target = mul;
	// 	add.B.Target = const0;

	// 	titleDrive.Value.Target = add;
	// }

	// public static void SetupInputs(Slot newUI, Slot overlapping)
	// {
	// 	Slot PossibleVerticalLayout = overlapping.FindChild("Vertical Layout");
	// 	if (PossibleVerticalLayout != null)
	// 	{
	// 		Slot VerticalAlignerRoot = newUI.AddSlot("Aligner");
	// 		var AlignerGrid = VerticalAlignerRoot.AttachComponent<ObjectGridAligner>();
	// 		AlignerGrid.ItemsPerRow.Value = 1;
	// 		AlignerGrid.CellSize.Value = new(0f, 0.1f);
	// 		AlignerGrid.HorizontalAlignment.Value = ObjectGridAligner.Align.Mid;
	// 		AlignerGrid.VerticalAlignment.Value = ObjectGridAligner.Align.Mid;
	// 		AlignerGrid.RowAxis.Value = ObjectGridAligner.AxisDir.Xpos;
	// 		AlignerGrid.ColumnAxis.Value = ObjectGridAligner.AxisDir.Yneg;
	// 		AlignerGrid.AutoAddChildren.Value = true;

	// 		VerticalAlignerRoot.LocalPosition = new(0f, 0f, -6f);
	// 		VerticalAlignerRoot.LocalScale = new(400f, 400f, 400f);

	// 		if (PossibleVerticalLayout.GetComponent<PrimitiveMemberEditor>() != null)
	// 		{
	// 			List<Slot> children = [.. PossibleVerticalLayout.Children];
	// 			for (int i = 0; i < children.Count; i++)
	// 			{
	// 				Slot inputSlot = children[i];
	// 				if (!inputSlot.ActiveSelf) continue;
	// 				var inputText = inputSlot.Children.First().GetComponent<Text>().Content;
	// 				Slot inputField = VerticalAlignerRoot.AddSlot("Field Text");
	// 				var inputFieldRenderer = inputField.AttachComponent<TextRenderer>();
	// 				inputFieldRenderer.Text.DriveFrom(inputText);
	// 				inputFieldRenderer.Bounded.Value = true;
	// 				inputFieldRenderer.BoundsSize.Value = new(0.4f, 0.05f);
	// 				inputFieldRenderer.HorizontalAutoSize.Value = true;
	// 				inputFieldRenderer.VerticalAutoSize.Value = true;
	// 				inputFieldRenderer.BoundsAlignment.Value = Alignment.TopCenter;
	// 				inputFieldRenderer.VerticalAlign.Value = Elements.Assets.TextVerticalAlignment.Top;
	// 				Slot inputFieldBG = inputField.AddSlot("BG");
	// 				inputFieldBG.LocalScale = new(1, 1, 0.5f);
	// 				inputFieldBG.LocalPosition = new(0, -0.015f, 0.01f);
	// 				var inputFieldBGMesh = inputFieldBG.AttachComponent<BevelBoxMesh>();
	// 				inputFieldBGMesh.Size.Value = new(0.2f, 0.1f, 0.03f);
	// 				inputFieldBGMesh.Bevel.Value = 5;
	// 				var inputFieldBGMaterial = inputFieldBG.AttachComponent<PBS_Metallic>();
	// 				inputFieldBGMaterial.AlbedoColor.Value = new(0.8f);
	// 				var inputFieldBGRenderer = inputFieldBG.AttachComponent<MeshRenderer>();
	// 				inputFieldBGRenderer.Material.Target = inputFieldBGMaterial;
	// 				inputFieldBGRenderer.Mesh.Target = inputFieldBGMesh;
	// 			}
	// 		}
	// 		if (PossibleVerticalLayout.GetComponent<RefEditor>() != null)
	// 		{
	// 			var inputTextContent = PossibleVerticalLayout.Children.First().Children.ToList()[2].Children.First().GetComponent<Text>().Content;
	// 			Slot inputField = VerticalAlignerRoot.AddSlot("Ref Field");
	// 			Slot inputText = inputField.AddSlot("Ref Text");
	// 			var inputFieldRenderer = inputText.AttachComponent<TextRenderer>();
	// 			inputFieldRenderer.Text.DriveFrom(inputTextContent);
	// 			inputFieldRenderer.Bounded.Value = true;
	// 			inputFieldRenderer.BoundsSize.Value = new(0.4f, 0.05f);
	// 			inputFieldRenderer.HorizontalAutoSize.Value = true;
	// 			inputFieldRenderer.VerticalAutoSize.Value = true;
	// 			inputFieldRenderer.BoundsAlignment.Value = Alignment.TopCenter;
	// 			inputFieldRenderer.VerticalAlign.Value = Elements.Assets.TextVerticalAlignment.Top;

	// 			var bgMaterial = inputField.AttachComponent<PBS_Metallic>();
	// 			bgMaterial.AlbedoColor.Value = new(0.8f);

	// 			{
	// 				Slot inputFieldBG = inputField.AddSlot("Ref");
	// 				inputFieldBG.LocalScale = new(1, 1, 0.5f);
	// 				inputFieldBG.LocalPosition = new(0.036f, -0.015f, 0.01f);
	// 				var inputFieldBGMesh = inputFieldBG.AttachComponent<BevelBoxMesh>();
	// 				inputFieldBGMesh.Size.Value = new(0.52f, 0.1f, 0.03f);
	// 				inputFieldBGMesh.Bevel.Value = 5;
	// 				var inputFieldBGRenderer = inputFieldBG.AttachComponent<MeshRenderer>();
	// 				inputFieldBGRenderer.Material.Target = bgMaterial;
	// 				inputFieldBGRenderer.Mesh.Target = inputFieldBGMesh;
	// 			}
	// 			{
	// 				Slot resetButton = inputField.AddSlot("Reset");
	// 				resetButton.LocalScale = new(1, 1, 0.5f);
	// 				resetButton.LocalPosition = new(0.33f, -0.015f, 0.01f);
	// 				var resetButtonBGMesh = resetButton.AttachComponent<BevelBoxMesh>();
	// 				resetButtonBGMesh.Size.Value = new(0.065f, 0.1f, 0.03f);
	// 				resetButtonBGMesh.Bevel.Value = 5;
	// 				var resetButtonBGRenderer = resetButton.AttachComponent<MeshRenderer>();
	// 				resetButtonBGRenderer.Material.Target = bgMaterial;
	// 				resetButtonBGRenderer.Mesh.Target = resetButtonBGMesh;
	// 			}
	// 			{
	// 				Slot inspectorButton = inputField.AddSlot("Inspector");
	// 				inspectorButton.LocalScale = new(1, 1, 0.5f);
	// 				inspectorButton.LocalPosition = new(-0.33f, -0.015f, 0.01f);
	// 				var inspectorButtonMesh = inspectorButton.AttachComponent<BevelBoxMesh>();
	// 				inspectorButtonMesh.Size.Value = new(0.065f, 0.1f, 0.03f);
	// 				inspectorButtonMesh.Bevel.Value = 5;
	// 				var inspectorButtonRenderer = inspectorButton.AttachComponent<MeshRenderer>();
	// 				inspectorButtonRenderer.Material.Target = bgMaterial;
	// 				inspectorButtonRenderer.Mesh.Target = inspectorButtonMesh;
	// 			}
	// 			{
	// 				Slot workerButton = inputField.AddSlot("Worker");
	// 				workerButton.LocalScale = new(1, 1, 0.5f);
	// 				workerButton.LocalPosition = new(-0.26f, -0.015f, 0.01f);
	// 				var workerButtonMesh = workerButton.AttachComponent<BevelBoxMesh>();
	// 				workerButtonMesh.Size.Value = new(0.065f, 0.1f, 0.03f);
	// 				workerButtonMesh.Bevel.Value = 5;
	// 				var workerButtonRenderer = workerButton.AttachComponent<MeshRenderer>();
	// 				workerButtonRenderer.Material.Target = bgMaterial;
	// 				workerButtonRenderer.Mesh.Target = workerButtonMesh;
	// 			}
	// 		}
	// 		if (PossibleVerticalLayout.Children.First().Name == "Panel")
	// 		{
	// 			Slot panelSlot = PossibleVerticalLayout.Children.First();
	// 			if (panelSlot.ChildrenCount > 0 && panelSlot.Children.First().Name == "Image")
	// 			{
	// 				// probably a bool?
	// 				Slot inputField = VerticalAlignerRoot.AddSlot("Bool Field");

	// 				Slot toggleSlot = panelSlot.Children.First();

	// 				// if replacing with a ButtonToggle and a custom collider
	// 				//IField<bool> targetField = toggleSlot.GetComponent<Checkbox>().TargetState.Target;

	// 				FieldDrive<bool> fieldDrive = toggleSlot.GetComponent<Checkbox>().CheckVisual;

	// 				var bgMaterial = inputField.AttachComponent<PBS_Metallic>();

	// 				var colorDriver = inputField.AttachComponent<BooleanValueDriver<colorX>>();
	// 				colorDriver.TrueValue.Value = RadiantUI_Constants.Hero.GREEN;
	// 				colorDriver.FalseValue.Value = RadiantUI_Constants.Hero.RED;
	// 				fieldDrive.Target = colorDriver.State;

	// 				colorDriver.TargetField.Target = bgMaterial.AlbedoColor;

	// 				Slot toggleButton = inputField.AddSlot("Bool Toggle");
	// 				toggleButton.LocalScale = new(1, 1, 0.5f);
	// 				toggleButton.LocalPosition = new(-0.07f, -0.015f, 0.01f);
	// 				var toggleButtonMesh = toggleButton.AttachComponent<BevelBoxMesh>();
	// 				toggleButtonMesh.Size.Value = new(0.1f, 0.1f, 0.03f);
	// 				toggleButtonMesh.Bevel.Value = 5;
	// 				var toggleButtonRenderer = toggleButton.AttachComponent<MeshRenderer>();
	// 				toggleButtonRenderer.Material.Target = bgMaterial;
	// 				toggleButtonRenderer.Mesh.Target = toggleButtonMesh;
	// 			}
	// 		}
	// 	}
	// }

	// public static void SetupConnectors(ProtoFluxNodeVisual root, Slot overlapping, ValueMultiDriver<bool> invertedIsOverviewMulti)
	// {
	// 	Slot inputOperations = overlapping.FindChild("Inputs & Operations");
	// 	Slot outputImpulses = overlapping.FindChild("Outputs & Impulses");
	// 	List<Slot> AllInputOutputs = [.. inputOperations.Children, .. outputImpulses.Children];
	// 	List<Slot> AllChildren = [];
	// 	foreach (Slot slot in AllInputOutputs)
	// 	{
	// 		var listManager = slot.GetComponent<ProtoFluxDynamicElementManager>();
	// 		if (listManager == null)
	// 		{
	// 			AllChildren.Add(slot);
	// 			continue;
	// 		}
	// 		AllChildren.AddRange(Traverse.Create(listManager).Field<SyncRefList<Slot>>("_elements").Value);
	// 	}

	// 	// what the fuck
	// 	for (int i = 0; i < AllChildren.Count; i++)
	// 	{
	// 		Slot Element = AllChildren[i];
	// 		Slot Connector = Element.FindChild("Connector");
	// 		if (Connector == null) continue;
	// 		ProtoFluxElementProxy ConnectorElementProxy = Connector.GetComponent<ProtoFluxElementProxy>();
	// 		colorX ConnectorColor = ConnectorElementProxy.WireColor;
	// 		string ElementName = ConnectorElementProxy.IsDynamic ? ConnectorElementProxy.Index.ToString() : ConnectorElementProxy.ElementName.Value;
	// 		RectTransform connectorRect = Connector.GetComponent<RectTransform>();
	// 		Rect connectorGlobalRect = connectorRect.ComputeGlobalComputeRect();
	// 		float2 connectorCenter = connectorGlobalRect.Center;
	// 		float3 connectorLocalPosition = new(connectorCenter, 0);

	// 		Slot newConnector = Connector.Children.First().AddSlot(ElementName);
	// 		//newConnector.LocalPosition = connectorLocalPosition;
	// 		newConnector.LocalScale = new(30f, 30f, 30f);
	// 		var newConnectorMesh = newConnector.AttachComponent<BevelBoxMesh>();
	// 		newConnectorMesh.Bevel.Value = 0.1f;
	// 		var connectorMaterial = newConnector.AttachComponent<PBS_Metallic>();
	// 		connectorMaterial.AlbedoColor.Value = ConnectorColor;
	// 		var connectorRenderer = newConnector.AttachComponent<MeshRenderer>();
	// 		connectorRenderer.Mesh.Target = newConnectorMesh;
	// 		connectorRenderer.Material.Target = connectorMaterial;

	// 		var connectorCollider = newConnector.AttachComponent<BoxCollider>();

	// 		var newCanvas = newConnector.AttachComponent<Canvas>();
	// 		newCanvas.Size.Value = new(1f, 1f);

	// 		var connectorProxyComponent = newConnector.DuplicateComponent(ConnectorElementProxy);
	// 		var connectorUIComponent = newConnector.AttachComponent<ProtoFluxNodeVisual>();
	// 		connectorUIComponent.Node.Target = root.Node.Target;

	// 		bool isInput = newConnector.FindParent(s => s == inputOperations, 5) != null;

	// 		Slot newConnectorTitleBG = newConnector.AddSlot("Title BG");
	// 		Slot newConnectorTitle = newConnectorTitleBG.AddSlot("Title");

	// 		var el = invertedIsOverviewMulti.Drives.Add();
	// 		el.Target = newConnectorTitleBG.ActiveSelf_Field;

	// 		newConnectorTitleBG.LocalScale = new(1f, 1f, 0.5f);
	// 		newConnectorTitleBG.LocalPosition = new(isInput ? 2f : -2f, isInput ? 0.265f : -0.265f, -0.1f);

	// 		newConnectorTitle.LocalScale = new(3f, 3f, 3f);
	// 		newConnectorTitle.LocalPosition = new(0, 0, -0.13f);

	// 		var titleBGMesh = newConnectorTitleBG.AttachComponent<BevelBoxMesh>();
	// 		titleBGMesh.Bevel.Value = 0.05f;
	// 		titleBGMesh.Size.Value = new(3f, 0.5f, 0.25f);
	// 		var titleBGMaterial = newConnectorTitleBG.AttachComponent<PBS_Metallic>();
	// 		titleBGMaterial.AlbedoColor.Value = RadiantUI_Constants.Neutrals.MIDLIGHT;
	// 		var titleBGRenderer = newConnectorTitleBG.AttachComponent<MeshRenderer>();
	// 		titleBGRenderer.Mesh.Target = titleBGMesh;
	// 		titleBGRenderer.Material.Target = titleBGMaterial;

	// 		var titleRenderer = newConnectorTitle.AttachComponent<TextRenderer>();
	// 		titleRenderer.Bounded.Value = true;
	// 		titleRenderer.BoundsSize.Value = new(0.95f, 0.13f);
	// 		titleRenderer.Text.Value = ElementName;
	// 		titleRenderer.HorizontalAlign.Value = isInput ? Elements.Assets.TextHorizontalAlignment.Left : Elements.Assets.TextHorizontalAlignment.Right;
	// 	}
	// }

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(ProtoFluxDynamicElementManager), "AddElement")]
	// public static void ProtoFluxDynamicElementManagerAddPatch(IButton __instance)
	// {
	// 	if (LenowoTweeks.threedprotoflux.Value)
	// 	{
	// 		__instance.Slot.RunInUpdates(2, () =>
	// 		{
	// 			Slot Root = __instance.Slot.GetObjectRoot().FindChild("<NODE_UI>");
	// 			Slot NodeNameSlot = Root.GetChildrenWithTag("NodeName")[0];
	// 			var Operands = Root.FindChildInHierarchy("Inputs & Operations").FindChild("Operands");
	// 			var UIXAdd = Operands.FindChildInHierarchy("Horizontal Layout").GetAllChildren()[0].GetComponent<RectTransform>().ComputeGlobalComputeRect();
	// 			var UIXRemove = Operands.FindChildInHierarchy("Horizontal Layout").GetAllChildren()[1].GetComponent<RectTransform>().ComputeGlobalComputeRect();

	// 			var UIXName = Root.GetAllChildren()[1].FindChild("Text").GetComponent<RectTransform>().ComputeGlobalComputeRect();
	// 			NodeNameSlot.LocalPosition = new float3(0, UIXName.position.y, -3);
	// 			Root.FindChildInHierarchy("Add Button").LocalPosition = new float3(UIXAdd.x + 8, UIXAdd.y + 8, -3);
	// 			Root.FindChildInHierarchy("Remove Button").LocalPosition = new float3(UIXRemove.x + 24, UIXRemove.y + 6, -3);

	// 		});
	// 	}
	// }


	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(ProtoFluxDynamicElementManager), "RemoveElement")]
	// public static void ProtoFluxDynamicElementManagerRemovePatch(IButton __instance)
	// {
	// 	if (LenowoTweeks.threedprotoflux.Value)
	// 	{
	// 		__instance.Slot.RunInUpdates(2, () =>
	// 		{
	// 			Slot Root = __instance.Slot.GetObjectRoot().FindChild("<NODE_UI>");
	// 			Slot NodeNameSlot = Root.GetChildrenWithTag("NodeName")[0];
	// 			var Operands = Root.FindChildInHierarchy("Inputs & Operations").FindChild("Operands");
	// 			var UIXAdd = Operands.FindChildInHierarchy("Horizontal Layout").GetAllChildren()[0].GetComponent<RectTransform>().ComputeGlobalComputeRect();
	// 			var UIXRemove = Operands.FindChildInHierarchy("Horizontal Layout").GetAllChildren()[1].GetComponent<RectTransform>().ComputeGlobalComputeRect();

	// 			var UIXName = Root.GetAllChildren()[1].FindChild("Text").GetComponent<RectTransform>().ComputeGlobalComputeRect();
	// 			NodeNameSlot.LocalPosition = new float3(0, UIXName.position.y, -3);
	// 			Root.FindChildInHierarchy("Add Button").LocalPosition = new float3(UIXAdd.x + 8, UIXAdd.y + 8, -3);
	// 			Root.FindChildInHierarchy("Remove Button").LocalPosition = new float3(UIXRemove.x + 24, UIXRemove.y + 6, -3);

	// 		});
	// 	}
	// }

}

#endif
