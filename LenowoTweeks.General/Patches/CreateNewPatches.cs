using HarmonyLib;
using FrooxEngine;

using System.Collections;
using System.Reflection;
using FrooxEngine.UIX;
using Elements.Core;

namespace LenowoTweeks.General;

[HarmonyPatch]
public static class CreateNewPatches
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(DevCreateNewForm), "OpenCategory")]
	public static void SetupOrRemoveCategories()
	{
		var categoryKeys = (IDictionary)Traverse.Create(typeof(DevCreateNewForm)).Field("root").Field("_subcategories").GetValue();
		var curKeys = (IEnumerable<string>)categoryKeys.Keys;
		if (LenowoTweeks_General.smallPrimitives.Value)
		{
			if (!curKeys.Contains("Mod:Small3DModels"))
			{
				DevCreateNewForm.AddAction("/Mod:Small3DModels", "Box", (slot) =>
				{
					DevCreateNewForm.SpawnMesh(slot, typeof(BoxMesh));
					slot.GetComponent<BoxMesh>().Size.Value = float3.One * 0.1f;
				});
				DevCreateNewForm.AddAction("/Mod:Small3DModels", "Sphere", (slot) =>
				{
					DevCreateNewForm.SpawnMesh(slot, typeof(SphereMesh));
					slot.GetComponent<SphereMesh>().Radius.Value = 0.05f;
				});
				DevCreateNewForm.AddAction("/Mod:Small3DModels", "Quad", (slot) =>
				{
					DevCreateNewForm.SpawnMesh(slot, typeof(QuadMesh));
					slot.GetComponent<QuadMesh>().Size.Value = float2.One * 0.1f;
				});
			}
		}
		else
		{
			List<string> toRemove = [.. curKeys.Where(s => s.StartsWith("Mod:"))];
			foreach (var key in toRemove)
			{
				categoryKeys.Remove(key);
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(DevCreateNewForm), "OpenCategory")]
	public static void OnCreateNew(DevCreateNewForm __instance, string path, SyncRef<Slot> ____contentRoot)
	{
		if (!LenowoTweeks_General.smallPrimitives.Value) return;

		var contentRoot = ____contentRoot.Target;
		var actualContent = contentRoot[0][0][0]; // always in a static place

		var truePath = path ?? "";
		var trueRoot = __instance.CategoryRoot.Value ?? "";

		bool isRoot = truePath == trueRoot;

		if (isRoot)
		{
			foreach (var child in actualContent.Children)
			{
				var text = child[0].GetComponent<Text>().Content;
				if (text.Value.StartsWith("Mod:"))
				{
					text.Value = text.Value switch
					{
						"Mod:Small3DModels" => "Small 3D Models",
						_ => text.Value
					};
					child.OrderOffset = -1;
				}
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(DevCreateNewForm), "SpawnMesh")]
	public static void AfterMeshSpawn(Slot slot)
	{
		if (!LenowoTweeks_General.removeMeshDefaultGizmo.Value) return;
		slot.GetComponent<GizmoLink>().RemoveGizmo();
	}
}
