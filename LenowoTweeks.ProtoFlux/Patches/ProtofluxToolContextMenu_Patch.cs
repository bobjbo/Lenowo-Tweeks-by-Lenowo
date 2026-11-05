using System.Reflection;
using System.Reflection.Emit;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.FrooxEngine.ProtoFlux.CoreNodes;
using FrooxEngine.ProtoFlux;

using HarmonyLib;

using LenowoTweeks.Core;

namespace LenowoTweeks.ProtoFlux.Patches;



[HarmonyPatch]
public class ProtofluxToolContextMenu_Patch
{

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxTool), "GenerateMenuItems")]
	public static void GenerateMenuItems_Postfix(ProtoFluxTool __instance, ContextMenu menu)
	{
		ProtoFluxNode targetNode = GetHit(__instance)?.Collider?.Slot?.GetComponentInParents<ProtoFluxNode>();
		if (targetNode != null)
		{

			if (LenowoTweeks_ProtoFlux.InspectNodeShortcut.Value)
			{
				var newItem = menu.AddItem("Open Inspector On Node", OfficialAssets.Graphics.Icons.Tool.InspectorPanel, new colorX?(colorX.White));
				newItem.Button.LocalPressed += (_, _) =>
				{
					targetNode.OpenInspectorForTarget();
				};
			}

			if (LenowoTweeks_ProtoFlux.InspectNodeSource.Value)
			{
				IWorldElement? possibleTarget = null;
				Type nodeType = targetNode.GetType();
				Type baseType = nodeType.IsGenericType ? nodeType.GetGenericTypeDefinition() : nodeType;
				if (baseType == typeof(ValueSource<>) || baseType == typeof(ObjectValueSource<>) || baseType == typeof(ReferenceSource<>) || baseType == typeof(ElementSource<>))
				{
					possibleTarget = Traverse.Create(targetNode).Property<ISyncRef>("RootSourceReference").Value.Target;
				}
				else if (baseType == typeof(ValueFieldDrive<>) || baseType == typeof(ObjectFieldDrive<>))
				{
					MethodInfo getRootProxy = nodeType.GetMethod("GetRootProxy");
					dynamic proxy = getRootProxy.Invoke(targetNode, new object[] { false });
					possibleTarget = proxy?.Drive.Target;

				}
				else if (baseType == typeof(ReferenceDrive<>))
				{
					MethodInfo _getRefProxy = typeof(ProtofluxToolContextMenu_Patch).GetMethod(nameof(GetRefProxy));
					Type nodeInnerType = nodeType.GetGenericArguments()[0];
					MethodInfo getRefProxy = _getRefProxy?.MakeGenericMethod(nodeInnerType);
					dynamic proxy = getRefProxy.Invoke(null, new object[] { targetNode });
					possibleTarget = proxy?.Drive.Target;
				}

				if (possibleTarget != null)
				{
					var newItem = menu.AddItem("Open Source", new Uri("resdb:///bce306daeb7aec97fee14ea9d6f3320c86fd9a102554167c520e5feb048aba94.png"), new colorX?(colorX.Orange));
					newItem.Button.LocalPressed += (_, _) =>
					{
						possibleTarget.OpenInspectorForTarget(openWorkerOnly: true);
						menu.Close();
					};
				}
			}

			// 500 nested if statements
			if (LenowoTweeks_ProtoFlux.AllowFluxVisualsOverride.Value)
			{
				User setUser = null;
				if (targetNode.Slot.Parent.Name == "Box - Child" && targetNode.Slot.Parent.Parent.Name == "Parenter")
				{
					// we are inside of gooberprint parenter likely. find if its actually a gp with a virtualparent thing
					var vp = targetNode.Slot.Parent.Parent.GetComponent<VirtualParent>();
					if (vp != null)
					{
						// we have a virtualparent. now just follow it back, get the gooberprint fluxroot, and find its user
						Slot vpTarget = vp.OverrideParent.Target;
						if (vpTarget != null)
						{
							Slot gpFluxRoot = vpTarget.Parent.Children.First();
							var dynSpace = gpFluxRoot.GetComponent<DynamicVariableSpace>(s => s.SpaceName.Value == "GooberPrint");
							if (dynSpace != null)
							{
								// ok yes this is a gooberprint :D
								// who is running it?

								dynSpace.TryReadValue("SetUser", out setUser);
							}
						}
					}
				}

				if (setUser == null)
				{
					var fluxVisual = targetNode.GetVisual();
					if (fluxVisual != null)
					{
						setUser = fluxVisual.GetAllocatingUser();
					}
				}

				if (setUser != null)
				{
					// if user is us, null out the value
					if (setUser == __instance.LocalUser) setUser = null;

					// FINALLY, check if setUser is not the same as the current config
					if (Helpers.GetConfigReference<User>(__instance.LocalUser, "Flux.OverrideVisuals") != setUser)
					{
						var newItem = menu.AddItem("Override Flux Visuals", (Uri)null, colorX.Cyan);
						newItem.Button.LocalPressed += (_, _) =>
						{
							Helpers.SetConfigReference<User>(__instance.LocalUser, "Flux.OverrideVisuals", setUser);
						};
					}
				}
			}
		}

		if (Helpers.GetConfigReference<User>(__instance.LocalUser, "Flux.OverrideVisuals") != null)
		{
			var newItem = menu.AddItem("Clear Visuals Override", (Uri)null, colorX.Cyan);
			newItem.Button.LocalPressed += (_, _) =>
			{
				Helpers.SetConfigReference<User>(__instance.LocalUser, "Flux.OverrideVisuals", null);
			};
		}
	}

	// this is some fucking bullshit (but it means that the main function is untouched in the end)
	// and also this suprisingly works really well
	[HarmonyTranspiler]
	[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
	public static IEnumerable<CodeInstruction> ProtofluxToolTranspiler(IEnumerable<CodeInstruction> codes)
	{
		MethodInfo wikiFunc = AccessTools.Method(typeof(Hyperlink), nameof(Hyperlink.AttachForWikiPage));
		var codes2 = new List<CodeInstruction>(codes);
		for (int i = 0; i < codes2.Count; i++)
		{
			var code = codes2[i];
			if (code.Calls(wikiFunc))
			{
				codes2[i] = new(OpCodes.Call, ((Delegate)FakeWikiFuncProtoflux).Method);
			}
			else if (code.opcode == OpCodes.Ldstr && (string)code.operand == "Tools.ProtoFlux.Unpack")
			{
				for (int j = i; j < codes2.Count; j++)
				{
					if (codes2[j].opcode == OpCodes.Callvirt && codes2[j].operand.ToString().StartsWith("FrooxEngine.ContextMenuItem AddRefItem[Slot]"))
					{
						codes2[j] = new(OpCodes.Call, ((Delegate)FakeAddRefItem<Slot>).Method);
						break;
					}

				}
			}
		}
		return codes2.AsEnumerable();
	}

	public static ContextMenuItem FakeAddRefItem<T>(ContextMenu menu, in LocaleString label, Uri icon, in colorX? color, ButtonEventHandler<T> action, T argument) where T : class, IWorldElement
	{
		if (argument is Slot slot)
		{
			bool isGreedyGoober = LenowoTweeks_ProtoFlux.GreedyGooberUnpack.Value;
			bool isNormalGoober = LenowoTweeks_ProtoFlux.AllowGooberUnpack.Value;
			bool isGoober = isNormalGoober || isGreedyGoober;
			string tag = slot.Tag;
			bool isGooberUnpack = isGoober && !string.IsNullOrWhiteSpace(tag) && tag.StartsWith("[") && tag.Contains("&");
			if (isGreedyGoober) isGooberUnpack |= tag == "GPFolder" || slot.FindChild(c => c.Tag == "GPFolder", 0) != null;
			string unpackInPrintMsg = $"GP Unpack <size=50%>{slot.Name}</size>";
			var newLabel = isGooberUnpack ? unpackInPrintMsg : label;
			var newColor = isGooberUnpack ? colorX.Purple : color;
			return menu.AddRefItem(in newLabel, icon, newColor, action, argument);
		}
		return menu.AddRefItem(in label, icon, color, action, argument);
	}

	public static Hyperlink? FakeWikiFuncProtoflux(Slot slot, Type type)
	{
		if (!LenowoTweeks_ProtoFlux.nohelp.Value) return Hyperlink.AttachForWikiPage(slot, type);
		slot.Destroy();
		return null;
	}

	public static FrooxEngine.ProtoFlux.CoreNodes.ReferenceDrive<T>.Proxy GetRefProxy<T>(ProtoFluxNode node) where T : class, IWorldElement
	{
		return node.Slot.GetComponent((global::FrooxEngine.ProtoFlux.CoreNodes.ReferenceDrive<T>.Proxy p) => p.Node.Target == node);
	}

	[HarmonyReversePatch]
	[HarmonyPatch(typeof(ProtoFluxTool), "IsValidStorageNode")]
	public static bool IsValidStorageNode(ProtoFluxTool instance, ProtoFluxReferenceProxy proxy, Func<Type, Type> nodeTypeFunc) => throw new NotImplementedException("It's a reverse patch, dummy!");

	[HarmonyReversePatch]
	[HarmonyPatch(typeof(Tool), "GetHit")]
	public static RaycastHit? GetHit(Tool instance) => throw new NotImplementedException("HIT GUHHH");
}
