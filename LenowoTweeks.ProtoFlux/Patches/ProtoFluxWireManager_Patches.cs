using FrooxEngine;
using FrooxEngine.ProtoFlux;

using HarmonyLib;

using LenowoTweeks.Core;

using Renderite.Shared;

namespace LenowoTweeks.ProtoFlux.Patches;

[HarmonyPatch]
public class ProtoFluxWireManager_Patches
{
	private static UI_UnlitMaterial WireMaterial(Slot assetsSlot, bool RunningIsLocal)
	{
		UI_UnlitMaterial ui_unlitMaterial = assetsSlot.GetComponentOrAttach<UI_UnlitMaterial>(out bool attached);

		StaticTexture2D staticTexture2D = ui_unlitMaterial.Slot.GetComponentOrAttach<StaticTexture2D>();
		if (RunningIsLocal)
		{
			staticTexture2D.URL.Value = LenowoTweeks_ProtoFlux.customProtofluxWireUIX.Value;
			staticTexture2D.WrapModeU.Value = LenowoTweeks_ProtoFlux.wireImageWrapMode.Value;
			staticTexture2D.FilterMode.Value = LenowoTweeks_ProtoFlux.wireTextureFilterMode.Value;

			Helpers.SetConfigVariable(assetsSlot.LocalUser, "Protoflux.WireColor", LenowoTweeks_ProtoFlux.wireTextureColor.Value, assetsSlot);
			if (attached)
			{
				Helpers.DriveFromVariable(assetsSlot.LocalUser, "Protoflux.WireColor", ui_unlitMaterial.Tint, assetsSlot);
			}
		}
		staticTexture2D.WrapModeV.Value = TextureWrapMode.Clamp;
		staticTexture2D.MipMaps.Value = false;

		ui_unlitMaterial.Texture.Target = staticTexture2D;


		ui_unlitMaterial.Sidedness.Value = Sidedness.Double;
		//unlitMaterial.UseVertexColors.Value = true;
		ui_unlitMaterial.BlendMode.Value = BlendMode.Alpha;
		ui_unlitMaterial.ZWrite.Value = ZWrite.On;
		ui_unlitMaterial.RenderQueue.Value = LenowoTweeks_ProtoFlux.wireRenderQueue.Value;

		return ui_unlitMaterial;
	}



	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxWireManager), "OnAttach")]
	public static void ModifyConnectorWireMaterial(ProtoFluxWireManager __instance)
	{
		if (!Helpers.ModShouldRun(__instance.Slot)) return;

		User overrideUser = Helpers.GetConfigReference<User>(__instance.LocalUser, "Flux.OverrideVisuals");
		if ((!LenowoTweeks_ProtoFlux.useCustomProtofluxConnections.Value && overrideUser == null) || (overrideUser != null && !Helpers.HasWireManager(overrideUser))) return;

		User runningUser = overrideUser ?? __instance.LocalUser;
		Slot wireManager = Helpers.GetWireManager(runningUser);
		UI_UnlitMaterial ui_unlit = WireMaterial(wireManager, runningUser.IsLocalUser);

		SyncRef<MeshRenderer> renderer = (SyncRef<MeshRenderer>)__instance.GetSyncMember("_renderer");
		renderer.Target.Material.Target = ui_unlit;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxWireManager), "OnChanges")]
	public static void OnChanges(ProtoFluxWireManager __instance, SyncRef<MeshRenderer> ____renderer, SyncRef<StripeWireMesh> ____wireMesh)
	{
		if (__instance.IsRemoved || ____renderer.IsRemoved || ____renderer.Target.IsRemoved || ____wireMesh.IsRemoved || ____wireMesh.Target.IsRemoved || __instance.Slot.IsRemoved)
		{
			return;
		}

		User overrideUser = Helpers.GetConfigReference<User>(__instance.LocalUser, "Flux.OverrideVisuals");
		if ((!LenowoTweeks_ProtoFlux.useCustomProtofluxConnections.Value && overrideUser == null) || (overrideUser != null && !Helpers.HasWireManager(overrideUser))) return;

		if (!Helpers.ModShouldRun(__instance.Slot)) return;

		User runningUser = overrideUser ?? __instance.LocalUser;

		Slot wireManager = Helpers.GetWireManager(runningUser);
		____renderer.Target.Material.Target = WireMaterial(wireManager, runningUser.IsLocalUser);
	}
}
