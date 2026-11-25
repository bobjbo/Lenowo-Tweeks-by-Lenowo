using Elements.Core;

using FrooxEngine;
using FrooxEngine.ProtoFlux;

using HarmonyLib;

using LenowoTweeks.Core;

namespace LenowoTweeks.ProtoFlux.Patches;

[HarmonyPatch]
public class ProtoFlux_GooberPrint
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ProtoFluxTool), "OnUnpack")]
	public static bool CheckUnpackInPrint(Slot root)
	{
		bool isGreedyGoober = LenowoTweeks_ProtoFlux.GreedyGooberUnpack.Value;
		bool isNormalGoober = LenowoTweeks_ProtoFlux.AllowGooberUnpack.Value;
		bool isGoober = isNormalGoober || isGreedyGoober;

		if (!isGoober) return true;

		string tag = root.Tag;
		bool isGooberUnpack = isGoober && !string.IsNullOrWhiteSpace(tag) && tag.StartsWith("[") && tag.Contains("&");
		if (isGreedyGoober) isGooberUnpack |= tag == "GPFolder" || root.FindChild(c => c.Tag == "GPFolder", 0) != null;

		if (!isGooberUnpack) return true;

		// goober

		root.StartTask(async () =>
		{
			Uri spawnUri = (await root.Cloud.Variables.Read<Uri>("U-1O0jo6sFCi0", "U-1O0jo6sFCi0.Rosa.GooberPrint")).Entity;

			Slot GooberPrint = root.LocalUserSpace.AddSlot("GooberPrint Spawner", false);

			GooberPrint.PositionInFrontOfUser(distance: 0f, scale: false);
			GooberPrint.Forward = root.LocalUserRoot.ViewRotation * new float3(0, 0, 1f);
			GooberPrint.Up = root.LocalUserRoot.Slot.Up;
			GooberPrint.GlobalPosition += 0.5f * GooberPrint.Forward;

			// "GooberPrint" is not gooberprint

			await Helpers.CloudSpawn(spawnUri, GooberPrint);

			// "GooberPrint" is now gooberprint???

			// Reset position back to the user because for some reason it doesnt always do that
			GooberPrint.PositionInFrontOfUser(distance: 0f, scale: false);
			GooberPrint.Forward = root.LocalUserRoot.ViewRotation * new float3(0, 0, 1f);
			GooberPrint.Up = root.LocalUserRoot.Slot.Up;
			GooberPrint.GlobalPosition += 0.5f * GooberPrint.Forward;

			if (GooberPrint == null || GooberPrint.IsRemoved) return;

			Slot GooberProcessing = GooberPrint.Children.First();


			GooberProcessing.WriteDynamicVariable<Slot>("GooberPrint/UnpackRoot", root);

			await new Updates(6);

			ProtoFluxHelper.DynamicImpulseHandler.TriggerDynamicImpulse(GooberProcessing, "GPUnpack", false, null);

			await new Updates(6);

			ProtoFluxHelper.DynamicImpulseHandler.TriggerDynamicImpulse(GooberProcessing, "GPSnapNodes", false, null);
		});

		return false;
	}
}
