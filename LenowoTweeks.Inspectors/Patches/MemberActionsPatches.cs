using System.Reflection;
using System.Reflection.Emit;

using FrooxEngine;
using FrooxEngine.Undo;

using HarmonyLib;

namespace LenowoTweeks.Inspectors.Patches;

[HarmonyPatch]
public class MemberActionsPatches
{
	private static Type? _targetType;

	public static MethodBase? TargetMethod()
	{
		MethodInfo? method = null;

		_targetType = AccessTools.InnerTypes(typeof(InspectorMemberActions)).FirstOrDefault(t =>
		{
			method = AccessTools.GetDeclaredMethods(t).FirstOrDefault(m => m.ReturnType == typeof(Task) && m.Name.Contains("Pressed"));
			return method != null;
		});

		return method == null ? null : AccessTools.AsyncMoveNext(method);
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> codes = [.. instructions];

		bool afterGetResult = false;
		int insertPoint = -1;

		for (int i = 0; i < codes.Count; i++)
		{
			if (codes[i].operand?.ToString()?.Contains("GetResult") ?? false)
			{
				afterGetResult = true;
				continue;
			}

			if (afterGetResult && (codes[i].opcode.Name?.StartsWith("stloc") ?? false))
			{
				insertPoint = i;
				break;
			}
		}

		FieldInfo targetField = AccessTools.Field(_targetType, "target");
		MethodInfo patchMethod = AccessTools.Method(typeof(MemberActionsPatches), nameof(GenerateMemberActions));

		List<CodeInstruction> injected =
		[
			new(OpCodes.Ldloc_2),
			new(OpCodes.Ldloc_1),
			new(OpCodes.Ldfld, targetField),
			new(OpCodes.Call, patchMethod)
		];

		codes.InsertRange(insertPoint + 1, injected);

		return codes;
	}

	private static void GenerateMemberActions(ContextMenu menu, ISyncMember target)
	{
		var world = menu.World;
		if (target is SyncRef<User> userField)
		{
			menu.AddItem("Local User", (Uri?)null, RadiantUI_Constants.Hero.PURPLE).Button.LocalPressed += (_, _) =>
			{
				userField.UndoableSet(world.LocalUser);
				menu.Close();
			};
			menu.AddItem("Host User", (Uri?)null, RadiantUI_Constants.Hero.PURPLE).Button.LocalPressed += (_, _) =>
			{
				userField.UndoableSet(world.HostUser);
				menu.Close();
			};
			menu.AddItem("Set User:", (Uri?)null, RadiantUI_Constants.Hero.PURPLE).Button.LocalPressed += (_, evData) =>
			{
				world.RootSlot.StartTask(async () =>
				{
					menu = await world.LocalUser.OpenContextMenu(userField, evData.source.Slot);
					foreach (var user in world.AllUsers)
					{
						var thisUser = user;
						menu.AddItem(thisUser.UserName, (Uri?)null, RadiantUI_Constants.Hero.PURPLE).Button.LocalPressed += (_, _) =>
						{
							userField.UndoableSet(thisUser);
							menu.Close();
						};
					}
				});
			};
			
		}
	}
}
