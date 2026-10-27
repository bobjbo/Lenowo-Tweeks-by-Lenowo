using System.Reflection;
using System.Reflection.Emit;

using Elements.Core;

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
		if (!LenowoTweeks_Inspectors.extraMemberActions.Value) return;
		try
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
			if (target is IField field && !field.IsDriven)
			{
				if (target is ISyncRef fieldRef)
				{
					if (fieldRef is IAssetRef assetRef)
					{
						menu.AddItem("Create Boolean Asset Driver", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
						{
							typeof(MemberActionsPatches).GetMethod(nameof(MakeBooleanAssetDriver)).MakeGenericMethod(assetRef.TargetType.GenericTypeArguments[0]).Invoke(null, [assetRef]);
							menu.Close();
						};
					}
					menu.AddItem("Create Boolean Reference Driver", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
					{
						typeof(MemberActionsPatches).GetMethod(nameof(MakeBooleanReferenceDriver)).MakeGenericMethod(fieldRef.TargetType).Invoke(null, [fieldRef]);
						menu.Close();
					};
					menu.AddItem("Create Override", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
					{
						typeof(MemberActionsPatches).GetMethod(nameof(MakeReferenceUserOverride)).MakeGenericMethod(fieldRef.TargetType).Invoke(null, [fieldRef]);
						menu.Close();
					};
				}
				else
				{
					var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(field.ValueType));

					if (coder.Property<bool>("SupportsLerp").Value) // gradient drivers
					{
						menu.AddItem("Create Gradient Driver", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
						{
							typeof(MemberActionsPatches).GetMethod(nameof(MakeGradientDriver)).MakeGenericMethod(field.ValueType).Invoke(null, [field]);
							menu.Close();
						};
					}
					if (coder.Property<bool>("IsEnginePrimitive").Value) // boolean value drivers
					{
						menu.AddItem("Create Boolean Value Driver", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
						{
							typeof(MemberActionsPatches).GetMethod(nameof(MakeBooleanValueDriver)).MakeGenericMethod(field.ValueType).Invoke(null, [field]);
							menu.Close();
						};
						menu.AddItem("Create Override", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
						{
							typeof(MemberActionsPatches).GetMethod(nameof(MakeValueUserOverride)).MakeGenericMethod(field.ValueType).Invoke(null, [field]);
							menu.Close();
						};
						if (LenowoTweeks_Inspectors.moreMemberActions.Value)
						{
							menu.AddItem("Create Numeric Override Gather", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
							{
								typeof(MemberActionsPatches).GetMethod(nameof(MakeNumericUserOverride)).MakeGenericMethod(field.ValueType).Invoke(null, [field]);
								menu.Close();
							};
							if (field.ValueType == typeof(bool))
							{
								menu.AddItem("Create Boolean Override Gather", (Uri?)null, RadiantUI_Constants.Hero.GREEN).Button.LocalPressed += (_, _) =>
								{
									typeof(MemberActionsPatches).GetMethod(nameof(MakeBooleanUserOverride)).Invoke(null, [field]);
									menu.Close();
								};
							}
						}
					}
				}
			}
		} catch (Exception e)
		{
			UniLog.Error($"LenowoTweeks // Exception in GenerateMemberActions\n{e}");
		}
	}

	public static void MakeGradientDriver<T>(IField<T> field)
	{
		try
		{
			field.World.BeginUndoBatch("Create Gradient Driver");

			var vgd = field.FindNearestParent<Slot>().AttachComponent<ValueGradientDriver<T>>();

			vgd.CreateSpawnUndoPoint();
			vgd.Points.Add().Value.Value = field.Value;
			var p2 = vgd.Points.Add();
			p2.Value.Value = field.Value;
			p2.Position.Value = 1f;
			vgd.Target.Target = field;

		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeGradientDriver:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}
	}

	// not incredible to have to have multiple versions of the same function, however it allows things to be easier to modify
	public static void MakeValueUserOverride<T>(IField<T> field)
	{
		try
		{
			field.World.BeginUndoBatch("Create Value Override");

			var vuo = field.FindNearestParent<Slot>().AttachComponent<ValueUserOverride<T>>();

			vuo.CreateSpawnUndoPoint();
			vuo.PersistentOverrides.Value = true;
			vuo.CreateOverrideOnWrite.Value = true;
			vuo.Default.Value = field.Value;
			vuo.Target.Target = field;

		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeValueUserOverride:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}
	}
	public static void MakeNumericUserOverride<T>(IField<T> field)
	{
		try
		{
			field.World.BeginUndoBatch("Create Numeric Override Gather");

			var vuo = field.FindNearestParent<Slot>().AttachComponent<NumericUserOverrideGather<T>>();

			vuo.CreateSpawnUndoPoint();
			vuo.PersistentOverrides.Value = true;
			vuo.CreateOverrideOnWrite.Value = true;
			vuo.Default.Value = field.Value;
			vuo.Target.Target = field;

		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeNumericUserOverride:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}
	}
	public static void MakeBooleanUserOverride(IField<bool> field)
	{
		try
		{
			field.World.BeginUndoBatch("Create Boolean Override Gather");

			var vuo = field.FindNearestParent<Slot>().AttachComponent<BooleanUserOverrideGather>();

			vuo.CreateSpawnUndoPoint();
			vuo.PersistentOverrides.Value = true;
			vuo.CreateOverrideOnWrite.Value = true;
			vuo.Default.Value = field.Value;
			vuo.Target.Target = field;
		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeBooleanUserOverride:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}
	}

	public static void MakeReferenceUserOverride<T>(SyncRef<T> field) where T : class, IWorldElement
	{
		try
		{
			field.World.BeginUndoBatch("Create Reference Override");

			var vuo = field.FindNearestParent<Slot>().AttachComponent<ReferenceUserOverride<T>>();

			vuo.CreateSpawnUndoPoint();
			vuo.PersistentOverrides.Value = true;
			vuo.CreateOverrideOnWrite.Value = true;
			vuo.Default.Target = field.Target;
			vuo.Target.Target = field;

		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeReferenceUserOverride:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}
	}

	public static void MakeBooleanValueDriver<T>(IField<T> field)
	{
		try
		{
			field.World.BeginUndoBatch("Create Boolean Value Driver");

			var bvd = field.FindNearestParent<Slot>().AttachComponent<BooleanValueDriver<T>>();

			bvd.CreateSpawnUndoPoint();
			bvd.FalseValue.Value = field.Value;
			bvd.TrueValue.Value = field.Value;
			bvd.TargetField.Target = field;

		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeBooleanValueDriver:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}
	}
	public static void MakeBooleanReferenceDriver<T>(SyncRef<T> field) where T : class, IWorldElement
	{
		try
		{
			field.World.BeginUndoBatch("Create Boolean Reference Driver");

			var bvd = field.FindNearestParent<Slot>().AttachComponent<BooleanReferenceDriver<T>>();

			bvd.CreateSpawnUndoPoint();
			bvd.FalseTarget.Target = field.Target;
			bvd.TrueTarget.Target = field.Target;
			bvd.TargetReference.Target = field;

		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeBooleanReferenceDriver:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}
	}
	public static void MakeBooleanAssetDriver<T>(AssetRef<T> field) where T : class, IAsset
	{
		try
		{
			field.World.BeginUndoBatch("Create Boolean Asset Driver");

			var bvd = field.FindNearestParent<Slot>().AttachComponent<BooleanAssetDriver<T>>();

			bvd.CreateSpawnUndoPoint();
			bvd.FalseTarget.Target = field.Target;
			bvd.TrueTarget.Target = field.Target;
			bvd.Target.Target = field;

		} catch (Exception e)
		{
			UniLog.Error($"Lenowo Tweaks / Exception in MakeBooleanAssetDriver:\n{e}");
		} finally
		{
			field.World.EndUndoBatch();
		}

	}
}
