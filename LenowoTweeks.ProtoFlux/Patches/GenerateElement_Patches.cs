using Elements.Core;

using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Casts;
using FrooxEngine.UIX;

using HarmonyLib;

using LenowoTweeks.Core;

using Renderite.Shared;

namespace LenowoTweeks.ProtoFlux.Patches;

[HarmonyPatch]
public class GenerateElement_Patches
{
	public static void GetOrCreateTexture(Slot root, Uri atlasUri, bool alt)
	{
		Slot ConnectorManager = Helpers.GetConnectorManager(root.LocalUser);
		Slot connectorAssets = ConnectorManager.FindChildOrAdd("Connector Assets");

		string textureName = alt ? "Connector.Texture.Alt" : "Connector.Texture.Normal";

		var textureSource = Helpers.GetConfigReferenceSource<IAssetProvider<ITexture2D>>(root.LocalUser, textureName, connectorAssets);
		IAssetProvider<ITexture2D> spriteTexture = textureSource.Target;
		if (spriteTexture != null && spriteTexture.FindNearestParent<Slot>() == connectorAssets)
		{
			var staticTex = (StaticTexture2D)spriteTexture;
			if (staticTex != null && staticTex.URL.Value != atlasUri)
			{
				staticTex.URL.Value = atlasUri;
			}
		}
		if (textureSource.Target == null)
		{
			StaticTexture2D staticTexture2D = connectorAssets.AttachTexture(atlasUri, getExisting: false, uncompressed: false, directLoad: false, evenNull: false, TextureWrapMode.Clamp);
			staticTexture2D.FilterMode.Value = LenowoTweeks_ProtoFlux.wireTextureFilterMode.Value;
			staticTexture2D.MipMaps.Value = false;
			Helpers.SetConfigReference<IAssetProvider<ITexture2D>>(root.LocalUser, textureName, staticTexture2D, connectorAssets);
		}
	}

	public static void ApplyConnector(Image image, Type elementType = null, bool output = false, IWorldElement targetConnection = null)
	{
		User overrideUser = Helpers.GetConfigReference<User>(image.LocalUser, "Flux.OverrideVisuals");
		User runningUser = overrideUser ?? image.LocalUser;
		if ((!LenowoTweeks_ProtoFlux.useCustomProtofluxConnections.Value && overrideUser == null) || (overrideUser != null && !Helpers.HasConnectorManager(overrideUser))) return;
		Slot assetsLocation = Helpers.GetModSharedSlot(runningUser);
		if (assetsLocation == null) return;
		Slot ConnectorStuff = Helpers.GetConnectorManager(runningUser);

		UI_UnlitMaterial UnlitMat = ConnectorStuff.GetComponentOrAttach<UI_UnlitMaterial>();

		UnlitMat.BlendMode.Value = BlendMode.Alpha;
		UnlitMat.Sidedness.Value = Sidedness.Double;
		UnlitMat.ZWrite.Value = ZWrite.On;
		UnlitMat.RenderQueue.Value = 3000;

		if (runningUser.IsLocalUser)
		{
			Helpers.SetConfigVariable(image.LocalUser, "Protoflux.ConnectorColor", LenowoTweeks_ProtoFlux.connectorTextureColor.Value, ConnectorStuff);
		}
		Helpers.DriveFromVariable(runningUser, "Protoflux.ConnectorColor", UnlitMat.Tint, ConnectorStuff);

		image.Material.Target = UnlitMat;

		int type = 4;
		if (elementType != null)
		{
			type = typeof(IVector).IsAssignableFrom(elementType) ? (elementType.GetVectorDimensions() - 1) : 0;
		}

		Slot spriteAssetSlot = ConnectorStuff.FindChildOrAdd("Sprites");

		var brd = image.Slot.AttachComponent<BooleanReferenceDriver<IAssetProvider<Sprite>>>();

		var e1 = Helpers.GetConfigReferenceSource<SpriteProvider>(runningUser, $"Connector.Sprite.Normal-{type}-{output}", spriteAssetSlot);
		var e2 = Helpers.GetConfigReferenceSource<SpriteProvider>(runningUser, $"Connector.Sprite.Alt-{type}-{output}", spriteAssetSlot);

		e1.Target ??= ConnectorStuff.FindChild($"Connector_{type}_{output}_False")?.GetComponent<SpriteProvider>() ?? null;
		e2.Target ??= ConnectorStuff.FindChild($"Connector_{type}_{output}_True")?.GetComponent<SpriteProvider>() ?? null;

		brd.TrueTarget.Target = e1.Target;
		brd.FalseTarget.Target = e2.Target;

		brd.TargetReference.Target = image.Sprite;
		var tsdSource = Helpers.GetTimeSineDriverSource(runningUser);

		if (runningUser.IsLocalUser)
		{
			float4 borders = new(0.5f);
			bool flippedCalc = LenowoTweeks_ProtoFlux.wireConnectorFlipUV.Value ? !output : output;
			Rect rect = new(flippedCalc ? 0.5f : 0f, 0.2f * (4 - type), 1f, 0.2f);

			Uri ConnectorURI = LenowoTweeks_ProtoFlux.customProtofluxConnectorUIX.Value;
			Uri ConnectorURI2 = LenowoTweeks_ProtoFlux.customProtofluxEmptyConnectorUIX.Value ?? LenowoTweeks_ProtoFlux.customProtofluxConnectorUIX.Value;
			GetOrCreateTexture(image.Slot, ConnectorURI, false);
			GetOrCreateTexture(image.Slot, ConnectorURI2, true);

			var tex1 = Helpers.GetConfigReference<IAssetProvider<ITexture2D>>(runningUser, "Connector.Texture.Normal");
			var tex2 = Helpers.GetConfigReference<IAssetProvider<ITexture2D>>(runningUser, "Connector.Texture.Alt");

			var multiDriverNormal = spriteAssetSlot.GetComponent<ReferenceMultiDriver<IAssetProvider<ITexture2D>>>(v => v.UpdateOrder == 0);
			multiDriverNormal ??= spriteAssetSlot.AttachComponent<ReferenceMultiDriver<IAssetProvider<ITexture2D>>>();

			var multiDriverAlt = spriteAssetSlot.GetComponent<ReferenceMultiDriver<IAssetProvider<ITexture2D>>>(v => v.UpdateOrder == 1);
			if (multiDriverAlt == null)
			{
				multiDriverAlt = spriteAssetSlot.AttachComponent<ReferenceMultiDriver<IAssetProvider<ITexture2D>>>();
				multiDriverAlt.UpdateOrder = 1;
			}

			Helpers.DriveFromReference<IAssetProvider<ITexture2D>>(runningUser, "Connector.Texture.Normal", multiDriverNormal.Reference);
			Helpers.DriveFromReference<IAssetProvider<ITexture2D>>(runningUser, "Connector.Texture.Alt", multiDriverAlt.Reference);

			if (e1.Target == null)
			{
				if (tex1 != null)
				{
					SpriteProvider newSpriteProvider = spriteAssetSlot.AttachComponent<SpriteProvider>();
					newSpriteProvider.Rect.Value = rect;
					newSpriteProvider.Borders.Value = borders;

					var newItem = multiDriverNormal.Drives.Add();
					newItem.Target = newSpriteProvider.Texture;

					brd.TrueTarget.Target = newSpriteProvider;

					e1.Target = newSpriteProvider;
				}
			}
			if (e2.Target == null)
			{
				if (tex2 != null)
				{
					SpriteProvider newSpriteProvider2 = spriteAssetSlot.AttachComponent<SpriteProvider>();
					newSpriteProvider2.Rect.Value = rect;
					newSpriteProvider2.Borders.Value = borders;

					var newItem = multiDriverAlt.Drives.Add();
					newItem.Target = newSpriteProvider2.Texture;

					brd.FalseTarget.Target = newSpriteProvider2;

					e2.Target = newSpriteProvider2;
				}
			}
		}


		if (targetConnection is ISyncRef nodeInput)
		{
			var inputProxy = image.Slot.GetComponent<ProtoFluxInputProxy>();
			var eqd = image.Slot.AttachComponent<ReferenceEqualityDriver<ProtoFluxWireManager>>();
			eqd.Invert.Value = true;
			if (inputProxy != null)
			{
				eqd.TargetReference.Target = inputProxy.Wire;
			}
			var impProxy = image.Slot.GetComponent<ProtoFluxImpulseProxy>();
			if (impProxy != null)
			{
				eqd.TargetReference.Target = impProxy.Wire;
			}
			eqd.Target.Target = brd.State;
			Helpers.DriveFromTSD(runningUser, eqd.EnabledField);
		}
		else if (targetConnection is INodeOutput nodeOutput)
		{
			var rl = image.Slot.GetComponentOrAttach<ReferenceList<ProtoFluxNode>>();
			if (rl.References.Count == 0) rl.References.Add(null);
			var eqDriver = SetupEqualityDriver(image.Slot, brd.State, rl.References.GetElement(0));
			Helpers.DriveFromTSD(runningUser, eqDriver.EnabledField);
			//brd.State.Value = true;
		}
		else if (targetConnection is INodeOperation nodeOperation)
		{
			var rl = image.Slot.GetComponentOrAttach<ReferenceList<ProtoFluxNode>>();
			if (rl.References.Count == 0) rl.References.Add(null);
			var eqDriver = SetupEqualityDriver(image.Slot, brd.State, rl.References.GetElement(0));
			Helpers.DriveFromTSD(runningUser, eqDriver.EnabledField);
			//brd.State.Value = true;
		}
	}

	private static ReferenceEqualityDriver<T> SetupEqualityDriver<T>(Slot s, IField<bool> bv, SyncRef<T> field) where T : class, IWorldElement
	{
		var eq = s.AttachComponent<ReferenceEqualityDriver<T>>();
		eq.Target.Target = bv;
		eq.TargetReference.Target = field;
		eq.Invert.Value = true;
		Helpers.DriveFromVariable(s.LocalUser, "TSD.Source", eq.EnabledField);
		return eq;
	}


	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNodeVisual), "GenerateInputElement")]
	public static void TweakInputElementVisual(ProtoFluxNodeVisual __instance, Type elementType, ISyncRef input, Slot __result)
	{
		ApplyConnector(__result.GetComponentInChildren<Image>(), elementType, targetConnection: input);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNodeVisual), "GenerateOutputElement")]
	public static void TweakOutputElementVisual(ProtoFluxNodeVisual __instance, Type elementType, INodeOutput output, Slot __result)
	{
		ApplyConnector(__result.GetComponentInChildren<Image>(), elementType, output: true, targetConnection: output);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNodeVisual), "GenerateImpulseElement")]
	public static void TweakImpulseElementVisual(ProtoFluxNodeVisual __instance, ISyncRef input, Slot __result)
	{
		ApplyConnector(__result.GetComponentInChildren<Image>(), null, output: true, targetConnection: input);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNodeVisual), "GenerateOperationElement")]
	public static void TweakOperationElementVisual(ProtoFluxNodeVisual __instance, INodeOperation operation, Slot __result)
	{
		ApplyConnector(__result.GetComponentInChildren<Image>(), targetConnection: operation);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ProtoFluxNodeVisual), "BuildUI")]
	public static void TweakNodeVisual(ProtoFluxNodeVisual __instance)
	{
		if (!LenowoTweeks_ProtoFlux.disableRelayBackground.Value) return;
		var node = __instance.Node.Target;
		if (node == null) return;
		var nodeType = node.GetType();
		if (nodeType == typeof(ContinuationRelay) || nodeType == typeof(CallRelay) || nodeType == typeof(AsyncCallRelay))
		{
			__instance.Slot.Children.First().ActiveSelf = false;
			return;
		}
		Type nodeBaseType = nodeType.BaseType;
		if (nodeBaseType.IsGenericType && nodeBaseType.GetGenericTypeDefinition() == typeof(ValueCast<,>))
		{
			__instance.Slot.Children.First().ActiveSelf = false;
		}
		if (!nodeType.IsGenericType) return;
		var genericType = nodeType.GetGenericTypeDefinition();
		if (
			genericType != typeof(ValueRelay<>) && genericType != typeof(ObjectRelay<>) &&
			genericType != typeof(ValueToObjectCast<>) && genericType != typeof(ObjectCast<,>) &&
			genericType != typeof(NullableToObjectCast<>) && genericType != typeof(ValueCast<,>)
		) return;
		__instance.Slot.Children.First().ActiveSelf = false;
	}
}
