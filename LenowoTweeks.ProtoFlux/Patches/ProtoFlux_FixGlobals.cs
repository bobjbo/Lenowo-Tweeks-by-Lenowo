using Elements.Core;

using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;

using HarmonyLib;

using LenowoTweeks.Core;


namespace LenowoTweeks.ProtoFlux.Patches;

[HarmonyPatch]
public class ProtoFlux_FixGlobals
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxGlobalRefProxy), "BuildUI")]
	public static void FixGlobalStringField(ProtoFluxGlobalRefProxy __instance, SyncRef<Button> ____proxyVisual)
	{
		if (!Helpers.ModShouldRun(__instance)) return;
		if (!LenowoTweeks_ProtoFlux.initializeProtofluxGlobals.Value) return;
		Type valueType = __instance.ValueType.Value;
		Type baseType = valueType.IsGenericType ? valueType.GetGenericTypeDefinition() : valueType;

		// DO NOT DO METHOD/FUNCTION PROXIES!!!
		if (valueType.IsGenericType && baseType.Name.Contains("Func") || baseType.Name.Contains("Action")) return;
		// LIKE SERIOUSLY DO NOT DO THEM
		if (__instance.Node.Target.NodeName.Contains("MethodProxy") || __instance.Node.Target.NodeName.Contains("FunctionProxy")) return;

		var proxy = __instance.EnsureProxy();

		Slot proxySlot = ____proxyVisual.Target.Slot;
		if (valueType == typeof(string))
		{
			TextField textField = proxySlot.EnsureSingleComponent<TextField>();
			textField.Text = proxySlot.GetComponentInChildren<Text>();
		}
		else
		{
			__instance.InvokeMethod("OnProxyButtonPressed", [____proxyVisual.Target, new ButtonEventData()]);
		}

		__instance.InvokeMethod("OnChanges");
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxRefProxy), "UpdateLabel")]
	public static void FixGlobalDelegateLabel(ProtoFluxRefProxy __instance, FieldDrive<string> ____label)
	{
		if (!LenowoTweeks_ProtoFlux.useProperDelegateNames.Value) return;
		if (__instance is not ProtoFluxGlobalRefProxy globalRef) return;
		var globalProxy = Traverse.Create(globalRef).Field<RelayRef<IGlobalValueProxy>>("_currentProxy").Value.Target;
		if (globalProxy == null) return;
		var proxyType = globalProxy.GetType();
		if (!proxyType.IsGenericType || proxyType.GetGenericTypeDefinition() != typeof(GlobalDelegate<>)) return;
		// globalProxy is a GlobalDelegate<T>, so update the label with the actual delegate name
		var actualDelegate = Traverse.Create(globalProxy).Field("Delegate").Property("Target").Property("Method").Property<string>("Name").Value;
		if (actualDelegate == null) return;
		var sourceElement = Traverse.Create(globalProxy).Field("Delegate").Field<IWorldElement>("_targetElement").Value;
		____label.Target.Value = $"{sourceElement.GetType().GetNiceName()}: {actualDelegate}";
	}
}
