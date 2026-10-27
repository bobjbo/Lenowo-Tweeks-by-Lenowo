using Elements.Core;

using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;

using HarmonyLib;

using LenowoTweeks.Core;

namespace LenowoTweeks.ProtoFlux.Patches;

[HarmonyPatch]
public class ProtoFluxVisual_Patches
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNodeVisual), "BuildUI")]
	public static void TweakNodeVisual(UIBuilder ui, ProtoFluxNodeVisual __instance)
	{
		if (!LenowoTweeks_ProtoFlux.collapsibleProtoflux.Value) return;

		try
		{
			Slot UISlot = __instance.Slot;
			ProtoFluxNode fluxNode = __instance.Node.Target;
			Slot fluxSlot = fluxNode.Slot;

			// Fetch the total connections on both the input and output sides.
			int totalInputCount = fluxNode.NodeInputCount + fluxNode.NodeInputLists.Sum(l => l.Count)
													+ fluxNode.NodeOperationCount + fluxNode.NodeOperationLists.Sum(l => l.Count);

			int totalOutputCount = fluxNode.NodeOutputCount + fluxNode.NodeOutputLists.Sum(l => l.Count)
													+ fluxNode.NodeImpulseCount + fluxNode.NodeImpulseLists.Sum(l => l.Count);

			int threshold = LenowoTweeks_ProtoFlux.collapseThreshold.Value;
			bool makeCollapsable = totalInputCount > threshold || totalOutputCount > threshold;
			// If either side is over the threshold, make the node collapsable.
			ValueTag<bool> toggle_field = null;
			if (makeCollapsable)
			{
				toggle_field = fluxSlot.GetComponent<ValueTag<bool>>(vf => vf.UpdateOrder == 7679);
				if (toggle_field == null)
				{
					toggle_field = fluxSlot.AttachComponent<ValueTag<bool>>();
					toggle_field.UpdateOrder = 7679; // Unique Identifier
					toggle_field.Value.Value = true;

					var old_toggle_field = fluxSlot.GetComponent<ValueField<bool>>(vf => vf.UpdateOrder == 7679);
					if (old_toggle_field != null)
					{
						toggle_field.Value.Value = old_toggle_field.Value.Value;
						old_toggle_field.Destroy();
					}
				}
			}

			var ol = UISlot.FindChild("Overlapping Layout");
			var connections = ol.FindChild("Inputs & Operations").Children.Concat(ol.FindChild("Outputs & Impulses").Children);

			foreach (Slot s in connections)
			{
				var bv = s.AttachComponent<ValueTag<bool>>();

				Slot connector = s.FindChild("Connector");
				if (connector == null || connector.IsRemoved) continue;
				ProtoFluxElementProxy? proxyComponent = connector.GetComponent<ProtoFluxElementProxy>();
				if (proxyComponent == null || proxyComponent.IsRemoved) continue;
				Type proxyType = proxyComponent.GetType();
				if (makeCollapsable)
				{
					// Set up components to deactivate node if it is not connected and the expanded toggle is off.
					var mbcd = s.AttachComponent<MultiBoolConditionDriver>();
					mbcd.Mode.Value = MultiBoolConditionDriver.ConditionMode.Any;
					mbcd.Target.Target = s.ActiveSelf_Field;
					mbcd.Conditions.Add().Field.Target = toggle_field.Value;
					mbcd.Conditions.Add().Field.Target = bv.Value;

					// note: BRD<Sprite> may not always exist, as it requries CustomProtofluxWires to be enabled.
					// possible fix: make some bool field somewhere that always exists?
					if (proxyType == typeof(ProtoFluxInputProxy) || proxyType == typeof(ProtoFluxImpulseProxy))
						bv.Value.DriveFrom(connector.GetComponent<BooleanReferenceDriver<IAssetProvider<Sprite>>>().State);
				}

				if (proxyType == typeof(ProtoFluxOutputProxy) || proxyType == typeof(ProtoFluxOperationProxy))
				{
					// could possibly be swapped to similar logic to above? connectors now have the BRD state set properly, so it may work
					// but still must be careful of the comment above, as the BRD may not exist (if wires are not custom)
					var rl = connector.GetComponentOrAttach<ReferenceList<ProtoFluxNode>>();
					if (rl.References.Count == 0) rl.References.Add(null);
					SetupEqualityDriver(s, bv, rl.References.GetElement(0));
				}
			}

			if (makeCollapsable)
			{
				// Collapsable Button
				ui.NestInto(UISlot);
				var buttonPanel = ui.Panel();
				ui.IgnoreLayout();
				buttonPanel.Slot.OrderOffset = 32769L;
				buttonPanel.AnchorMin.Value = new float2(0f);
				buttonPanel.AnchorMax.Value = new float2(0.4f);
				buttonPanel.OffsetMin.Value = new float2(8f);
				buttonPanel.OffsetMax.Value = new float2(-16f, 16f);
				Button addButton = ui.Button();
				ButtonToggle bt = addButton.Slot.AttachComponent<ButtonToggle>();
				bt.TargetValue.Target = toggle_field.Value;
				ui.NestOut();
				ui.NestOut();

				// Button Text
				var bvd = addButton.Slot.AttachComponent<BooleanValueDriver<string>>();
				bvd.TrueValue.Value = "-";
				bvd.FalseValue.Value = "+";
				bvd.TargetField.TrySet(addButton.LabelTextField);
				bvd.State.DriveFrom(toggle_field.Value);

				// possible todo: make collapsing only recalculate on collapse/expand?
				// would mean you can connect 3 inputs in playoneshot, collapse, disconnect them, and then plug anything else in later
				// may be helpful, but at the same time this also works as is.

				// If the field is collapsed, open it up and collapse it again to help reset wire field positions to the correct spot.
				if (!toggle_field.Value.Value)
				{
					UISlot.RunInUpdates(4, () =>
					{
						toggle_field.Value.Value = true;
						UISlot.RunInUpdates(2, () =>
						{
							toggle_field.Value.Value = false;
						});
					});
				}
			}
		} catch (Exception e)
		{
			UniLog.Error("Encountered an ERROR in TweakNodeVisual: " + e.Message + "\n" + e.StackTrace, false);
		}

	}


	private static ReferenceEqualityDriver<T> SetupEqualityDriver<T>(Slot s, ValueTag<bool> bv, SyncRef<T> field) where T : class, IWorldElement
	{
		var eq = s.AttachComponent<ReferenceEqualityDriver<T>>();
		eq.Target.Target = bv.Value;
		eq.TargetReference.Target = field;
		eq.Invert.Value = true;
		// if it sets up the equality driver, make sure that its actually updating
		Helpers.DriveFromClock(s.LocalUser, eq.EnabledField);
		return eq;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ProtoFluxNode), "TryConnectInput")]
	public static void TryConnectInputHookPre(ISyncRef input, out IWorldElement __state)
	{
		__state = input?.Target;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNode), "TryConnectInput")]
	public static void TryConnectInputHook(ISyncRef input, INodeOutput output, ref bool __result, IWorldElement __state)
	{
		try
		{
			if (__result)
			{
				Slot nodeSlot = output.Parent.GetType() == typeof(Slot) ? (Slot)output.Parent
					: output.Parent.Parent.GetType() == typeof(Slot) ? (Slot)output.Parent.Parent
					: (Slot)output.Parent.Parent.Parent;

				var proxy = nodeSlot.GetComponentInChildren<ProtoFluxOutputProxy>(p => p.NodeOutput.Target == output);
				if (proxy == null) return;

				ProtoFluxNode inputNode = input is ProtoFluxNode ? (ProtoFluxNode)input
					: input.Parent is ProtoFluxNode ? (ProtoFluxNode)input.Parent
					: (ProtoFluxNode)input.Parent.Parent;

				// Check and clear out the old connection
				if (__state != null && __state != output)
				{
					Slot prevNodeSlot = __state.Parent.GetType() == typeof(Slot) ? (Slot)__state.Parent
						: __state.Parent.Parent.GetType() == typeof(Slot) ? (Slot)__state.Parent.Parent
						: (Slot)__state.Parent.Parent.Parent;

					var prevProxy = prevNodeSlot.GetComponentInChildren<ProtoFluxOutputProxy>(p => p.NodeOutput.Target == __state);

					RemoveNodeFromField(prevProxy.Slot, inputNode);
				}

				AddNodeToField(proxy.Slot, inputNode);
			}
		} catch (Exception e)
		{
			UniLog.Error("Encountered an ERROR in TryConnectInputHook: " + e.Message + "\n" + e.StackTrace, false);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ProtoFluxNode), "TryConnectImpulse")]
	public static void TryConnectImpulseHookPre(ISyncRef impulse, out IWorldElement __state)
	{
		__state = impulse?.Target;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNode), "TryConnectImpulse")]
	public static void TryConnectImpulseHook(ISyncRef impulse, INodeOperation operation, ref bool __result, IWorldElement __state)
	{
		try
		{
			if (__result)
			{
				Slot nodeSlot = operation.Parent is Slot ? (Slot)operation.Parent
					: operation.Parent.Parent is Slot ? (Slot)operation.Parent.Parent
					: (Slot)operation.Parent.Parent.Parent;

				var proxy = nodeSlot.GetComponentInChildren<ProtoFluxOperationProxy>(p => p.NodeOperation.Target == operation);
				if (proxy == null) return;

				ProtoFluxNode impulseNode = impulse is ProtoFluxNode ? (ProtoFluxNode)impulse
					: impulse.Parent is ProtoFluxNode ? (ProtoFluxNode)impulse.Parent
					: (ProtoFluxNode)impulse.Parent.Parent;

				// Check and clear out the old connection
				if (__state != null && __state != operation)
				{
					Slot prevNodeSlot = __state.Parent.GetType() == typeof(Slot) ? (Slot)__state.Parent
						: __state.Parent.Parent.GetType() == typeof(Slot) ? (Slot)__state.Parent.Parent
						: (Slot)__state.Parent.Parent.Parent;

					var prevProxy = prevNodeSlot.GetComponentInChildren<ProtoFluxOperationProxy>(p => p.NodeOperation.Target == __state);

					RemoveNodeFromField(prevProxy.Slot, impulseNode);
				}

				AddNodeToField(proxy.Slot, impulseNode);
			}
		} catch (Exception e)
		{
			UniLog.Error("Encountered an ERROR in TryConnectImpulseHook: " + e.Message + "\n" + e.StackTrace, false);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ProtoFluxWireManager), "OnAwake")]
	public static void OnAwakeHook(ProtoFluxWireManager __instance)
	{
		try
		{
			__instance.Slot.RunInUpdates(LenowoTweeks_ProtoFlux.collapseAwakeDelay.Value, () =>
			{
				if (__instance?.Slot != null && __instance.Slot.Name != "TempWire"
					&& __instance?.ConnectPoint?.Target != null)
				{
					if (__instance.Slot.GetComponents<ProtoFluxWireManager>().Count == 1)
					{
						AddNodeToField(__instance.ConnectPoint.Target.Parent,
							__instance.Slot.Parent.FindParent(s => s.Name == "<NODE_UI>").Parent.GetComponent<ProtoFluxNode>());
					}
				}
			});
		} catch (Exception e)
		{
			UniLog.Error("Encountered an ERROR in OnDestroyHook: " + e.Message + "\n" + e.StackTrace, false);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ProtoFluxWireManager), "OnDestroy")]
	public static void OnDestroyHook(ProtoFluxWireManager __instance)
	{
		try
		{
			if (__instance?.Slot != null && __instance.Slot.Name != "TempWire"
				&& __instance?.ConnectPoint?.Target != null)
			{
				if (!Helpers.ModShouldRun(__instance.Slot)) return;

				if (__instance.Slot.GetComponents<ProtoFluxWireManager>().Count == 1)
				{
					RemoveNodeFromField(__instance.ConnectPoint.Target.Parent,
						__instance.Slot.Parent.FindParent(s => s.Name == "<NODE_UI>").Parent.GetComponent<ProtoFluxNode>());
				}
			}
		} catch (Exception e)
		{
			UniLog.Error("Encountered an ERROR in OnDestroyHook: " + e.Message + "\n" + e.StackTrace, false);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ProtoFluxTool), "OnPrimaryRelease")]
	public static void OnCutHook(ProtoFluxTool __instance, SyncRef<Slot> ____currentCutLine, HashSet<ProtoFluxWireManager> ____cutWires)
	{
		try
		{
			// this came from the protoflux tool itself
			// i just removed the 3 levels of nested if statements and put it into one
			if (____currentCutLine.Target != null && ____cutWires != null && ____cutWires.Count > 0)
			{
				foreach (ProtoFluxWireManager cutWire in ____cutWires)
				{
					var elProxy = cutWire.Slot.GetComponentInParents<ProtoFluxElementProxy>();

					if (elProxy is ProtoFluxInputProxy inProxy)
					{
						RemoveNodeFromField(
							inProxy.Wire.Target.ConnectPoint.Target.Parent,
							inProxy.Slot.Parent.FindParent(s => s.Name == "<NODE_UI>").Parent.GetComponent<ProtoFluxNode>()
						);
					}
					if (elProxy is ProtoFluxImpulseProxy impProxy)
					{
						RemoveNodeFromField(
							impProxy.Wire.Target.ConnectPoint.Target.Parent,
							impProxy.Slot.Parent.FindParent(s => s.Name == "<NODE_UI>").Parent.GetComponent<ProtoFluxNode>()
						);
					}
				}
			}
		} catch (Exception e)
		{
			UniLog.Error("Encountered an ERROR in OnCutHook: " + e.Message + "\n" + e.StackTrace, false);
		}
	}

	// todo: find a way to safely filter out all nulls/removed elements?
	// any element that is null you know doenst matter anymore. however, its possible for the element to be not null, but removed.
	public static void AddNodeToField(Slot proxySlot, ProtoFluxNode node)
	{
		var rl = proxySlot.GetComponent<ReferenceList<ProtoFluxNode>>();
		if (rl == null) return;

		int i = rl.References.FindIndex(n => n?.Target == node);
		if (i != -1) return;

		if (rl.References.Count == 0) rl.References.Add(null);

		if (rl.References[0] == null)
			rl.References[0] = node;
		else
			rl.References.Add(node);
	}

	public static void RemoveNodeFromField(Slot proxySlot, ProtoFluxNode node)
	{
		var rl = proxySlot.GetComponent<ReferenceList<ProtoFluxNode>>();
		if (rl == null) return;

		int i = rl.References.FindIndex(n => n?.Target == node);
		if (i == -1) return;

		if (rl.References.Count == 0)
			rl.References.Add(null);
		else if (rl.References.Count == 1)
			rl.References[0] = null;
		else if (i == 0)
		{
			rl.References[0] = rl.References.Last();
			rl.References.RemoveAt(rl.References.Count - 1);
		}
		else
			rl.References.RemoveAt(i);
	}
}
