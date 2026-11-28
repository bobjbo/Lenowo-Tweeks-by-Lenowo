using FrooxEngine;

using HarmonyLib;

namespace LenowoTweeks.General.Patches;

[HarmonyPatch]
public static class DontCheckOcclusion
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(SlotPositioning), nameof(SlotPositioning.GetPointInFrontOfUser))]
	public static void WhenGettingPointDontCheckOcclusion(ref bool checkOcclusion)
	{
		if (!LenowoTweeks_General.dontCheckOcclusion.Value) return;
		checkOcclusion = false;
	}
}
