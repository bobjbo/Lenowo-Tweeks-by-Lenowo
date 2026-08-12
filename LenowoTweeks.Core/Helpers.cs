using Elements.Core;

using FrooxEngine;

namespace LenowoTweeks.Core;

[Flags]
public enum RunMode
{
	None = 0,
	Always = 1,
	ElementAllocating = 2,
	SlotAllocating = 4,
	AllowNonAllocating = 16,
	AllowRemoved = 32
}

public static class Helpers
{
	const RunMode DefaultRunMode = RunMode.ElementAllocating | RunMode.SlotAllocating;

	// Overloads to make this able to be run on literally anything
	public static bool ModShouldRun(Component instance) => ModShouldRun(instance, DefaultRunMode);
	public static bool ModShouldRun(Component instance, RunMode runMode) => Internal_ModShouldRun(instance, instance.Slot, runMode);
	public static bool ModShouldRun(Slot instance) => ModShouldRun(instance, DefaultRunMode);
	public static bool ModShouldRun(Slot instance, RunMode runMode) => Internal_ModShouldRun(instance, instance, runMode);

	static bool Internal_ModShouldRun(IWorldElement element, Slot targetSlot, RunMode runMode)
	{
		if (runMode == RunMode.None) return false;
		if (runMode == RunMode.Always) return true;

		if (!runMode.HasFlag(RunMode.AllowRemoved) && (element.IsRemoved || targetSlot.IsRemoved)) return false;

		User allocatingUser = element.GetAllocatingUser();

		if (runMode.HasFlag(RunMode.ElementAllocating))
		{
			if (allocatingUser == targetSlot.LocalUser) return true;
			if (runMode.HasFlag(RunMode.AllowNonAllocating) && allocatingUser == null) return true;
		}
		if (runMode.HasFlag(RunMode.SlotAllocating))
		{
			if (allocatingUser == null)
			{
				User instanceAllocUser = targetSlot.GetAllocatingUser();
				if (runMode.HasFlag(RunMode.AllowNonAllocating) && instanceAllocUser == null) return true;
				if (instanceAllocUser == null || instanceAllocUser != targetSlot.LocalUser)
				{
					return false;
				}
				return true;
			}
		}

		return false;
	}

	public static Slot GetModSharedSlot(User runner)
	{
		return runner.World.RootSlot.FindChildOrAdd("__TEMP", persistent: false).FindChildOrAdd("LenowoTweeks Shared", persistent: false);
	}
	public static bool HasModUserSlot(User runner)
	{
		Slot modShared = GetModSharedSlot(runner);
		return modShared.FindChild(runner.UserName + "\'s Config") != null;
	}
	public static Slot GetModUserSlot(User runner)
	{
		Slot modShared = GetModSharedSlot(runner);
		return modShared.FindChildOrAdd(runner.UserName + "\'s Config", persistent: false);
	}

	public static bool HasProtoFluxManager(User runner)
	{
		if (!HasModUserSlot(runner)) return false;
		Slot userAssets = GetModUserSlot(runner);
		return userAssets.FindChild(runner.UserName + "\'s Protoflux Manager") != null;
	}

	public static Slot GetProtoFluxManager(User runner)
	{
		Slot userAssets = GetModUserSlot(runner);
		return userAssets.FindChildOrAdd(runner.UserName + "\'s Protoflux Manager", persistent: false);
	}

	public static bool HasConnectorManager(User runner)
	{
		if (!HasModUserSlot(runner)) return false;
		if (!HasProtoFluxManager(runner)) return false;
		Slot protofluxManager = GetProtoFluxManager(runner);
		return protofluxManager.FindChild(runner.UserName + "\'s Connector Stuff") != null;
	}
	public static Slot GetConnectorManager(User runner)
	{
		Slot protofluxManager = GetProtoFluxManager(runner);
		return protofluxManager.FindChildOrAdd(runner.UserName + "\'s Connector Stuff", persistent: false);
	}
	public static bool HasWireManager(User runner)
	{
		if (!HasModUserSlot(runner)) return false;
		if (!HasProtoFluxManager(runner)) return false;
		Slot protofluxManager = GetProtoFluxManager(runner);
		return protofluxManager.FindChild(runner.UserName + "\'s Wire Manager") != null;
	}
	public static Slot GetWireManager(User runner)
	{
		Slot protofluxManager = GetProtoFluxManager(runner);
		return protofluxManager.FindChildOrAdd(runner.UserName + "\'s Wire Manager", persistent: false);
	}

	public static DynamicVariableSpace GetConfigSpace(User runner)
	{
		Slot userConfig = GetModUserSlot(runner);
		DynamicVariableSpace variableSpace = userConfig.GetComponentOrAttach<DynamicVariableSpace>(out bool attached);
		// It should always be config, so this is fine
		variableSpace.SpaceName.Value = "Config";

		if (runner.IsLocalUser)
		{
			Slot versionsSlot = userConfig.FindChildOrAdd("Versions");
			foreach (var module in LenowoTweeks_Core.RegisteredModules)
			{
				versionsSlot.FindChildOrAdd(module.ModuleName).Tag = module.ModuleVersion;
			}
		}

		return variableSpace;
	}

	public static T GetConfigVariable<T>(User runner, string VariableName, T defaultValue = default)
	{
		string realVarName = "Config/" + VariableName;
		Slot configSlot = GetConfigSpace(runner).Slot;
		var AllVariables = configSlot.GetComponentsInChildren<DynamicValueVariable<T>>().DistinctBy(v => v.VariableName.Value).ToDictionary(v => v.VariableName.Value);
		if (AllVariables.TryGetValue(realVarName, out var variable)) return variable.Value.Value;
		return defaultValue;
	}

	public static T GetConfigReference<T>(User runner, string VariableName, T defaultValue = null) where T : class, IWorldElement
	{
		string realVarName = "Config/" + VariableName;
		Slot configSlot = GetConfigSpace(runner).Slot;
		var AllVariables = configSlot.GetComponentsInChildren<DynamicReferenceVariable<T>>().DistinctBy(v => v.VariableName.Value).ToDictionary(v => v.VariableName.Value);
		if (AllVariables.TryGetValue(realVarName, out var variable)) return variable.Reference.Target;
		return defaultValue;
	}

	public static IField<T> GetConfigVariableSource<T>(User runner, string VariableName, Slot createUnder = null, Action<Sync<T>>? OnCreate = null)
	{
		string realVarName = "Config/" + VariableName;
		Slot configSlot = GetConfigSpace(runner).Slot;
		var AllVariables = configSlot.GetComponentsInChildren<DynamicValueVariable<T>>().DistinctBy(v => v.VariableName.Value).ToDictionary(v => v.VariableName.Value);
		if (AllVariables.TryGetValue(realVarName, out var variable)) return variable.Value;
		Slot varSlot = createUnder ?? configSlot;
		var newVar = varSlot.AttachComponent<DynamicValueVariable<T>>();
		newVar.VariableName.Value = realVarName;
		OnCreate?.Invoke(newVar.Value);
		return newVar.Value;
	}

	public static SyncRef<T> GetConfigReferenceSource<T>(User runner, string VariableName, Slot createUnder = null, Action<SyncRef<T>>? OnCreate = null) where T : class, IWorldElement
	{
		string realVarName = "Config/" + VariableName;
		Slot configSlot = GetConfigSpace(runner).Slot;
		var AllVariables = configSlot.GetComponentsInChildren<DynamicReferenceVariable<T>>().DistinctBy(v => v.VariableName.Value).ToDictionary(v => v.VariableName.Value);
		if (AllVariables.TryGetValue(realVarName, out var variable)) return variable.Reference;
		Slot varSlot = createUnder ?? configSlot;
		var newVar = varSlot.AttachComponent<DynamicReferenceVariable<T>>();
		newVar.VariableName.Value = realVarName;
		OnCreate?.Invoke(newVar.Reference);
		return newVar.Reference;
	}

	public static void SetConfigVariable<T>(User runner, string VariableName, T NewValue, Slot createUnder = null, Action<Sync<T>>? OnCreate = null)
	{
		// allow for a user to define that they are, in fact, trying to override this variable and it should *not* be written to
		bool hasOverrideSet = GetConfigVariable<bool>(runner, VariableName + ".override", false);
		if (hasOverrideSet) return;
		string realVarName = "Config/" + VariableName;
		Slot configSlot = GetConfigSpace(runner).Slot;
		var AllVariables = configSlot.GetComponentsInChildren<DynamicValueVariable<T>>().DistinctBy(v => v.VariableName.Value).ToDictionary(v => v.VariableName.Value);
		if (AllVariables.TryGetValue(realVarName, out var variable)) variable.Value.Value = NewValue;
		else
		{
			Slot varSlot = createUnder ?? configSlot;
			var newVar = varSlot.AttachComponent<DynamicValueVariable<T>>();
			newVar.VariableName.Value = realVarName;
			newVar.Value.Value = NewValue;
			OnCreate?.Invoke(newVar.Value);
		}
	}

	public static void SetConfigReference<T>(User runner, string VariableName, T NewValue, Slot createUnder = null, Action<SyncRef<T>>? OnCreate = null) where T : class, IWorldElement
	{
		// allow for a user to define that they are, in fact, trying to override this variable and it should *not* be written to
		bool hasOverrideSet = GetConfigVariable<bool>(runner, VariableName + ".override", false);
		if (hasOverrideSet) return;
		string realVarName = "Config/" + VariableName;
		Slot configSlot = GetConfigSpace(runner).Slot;
		var AllVariables = configSlot.GetComponentsInChildren<DynamicReferenceVariable<T>>().DistinctBy(v => v.VariableName.Value).ToDictionary(v => v.VariableName.Value);
		if (AllVariables.TryGetValue(realVarName, out var variable)) variable.Reference.Target = NewValue;
		else
		{
			Slot varSlot = createUnder ?? configSlot;
			var newVar = varSlot.AttachComponent<DynamicReferenceVariable<T>>();
			newVar.VariableName.Value = realVarName;
			newVar.Reference.Target = NewValue;
			OnCreate?.Invoke(newVar.Reference);
		}
	}

	public static T GetOrCreateConfigVariable<T>(User runner, string VariableName, Slot createUnder = null, Action<Sync<T>>? OnCreate = null)
	{
		var source = GetConfigVariableSource<T>(runner, VariableName, createUnder, OnCreate);
		return source.Value;
	}

	public static T GetOrCreateConfigReference<T>(User runner, string VariableName, Slot createUnder = null, Action<SyncRef<T>>? OnCreate = null) where T : class, IWorldElement
	{
		var source = GetConfigReferenceSource<T>(runner, VariableName, createUnder, OnCreate);
		return source.Target;
	}

	public static void DriveFromVariable<T>(User runner, string VariableName, IField<T> output, Slot createUnder = null, Action<Sync<T>>? OnCreate = null)
	{
		var source = GetConfigVariableSource<T>(runner, VariableName, createUnder, OnCreate);
		if (source == null) return;
		output.DriveFrom(source);
	}

	public static void DriveFromReference<T>(User runner, string VariableName, SyncRef<T> output, Slot createUnder = null, Action<SyncRef<T>>? OnCreate = null) where T : class, IWorldElement
	{
		var source = GetConfigReferenceSource<T>(runner, VariableName, createUnder, OnCreate);
		if (source == null) return;
		output.DriveFrom(source);
	}

	public static Slot GetTimeSineDriverSlot(User runner)
	{
		return GetModUserSlot(runner).FindChildOrAdd("TSD");
	}
	public static IField<bool> GetTimeSineDriverSource(User runner)
	{
		Slot TSDS = GetTimeSineDriverSlot(runner);
		var tsd = TSDS.GetComponentOrAttach<TimeSineDriver>();
		var fvf = TSDS.GetComponentOrAttach<ValueField<float>>();
		var cid = TSDS.GetComponentOrAttach<ConvertibleIntDriver<float>>();
		var ivf = TSDS.GetComponentOrAttach<ValueField<int>>();
		var veqd = TSDS.GetComponentOrAttach<ValueEqualityDriver<int>>();
		tsd.Min.Value = 0;
		tsd.Max.Value = 1;
		tsd.Speed.Value = 4;
		tsd.Target.Target = fvf.Value;
		cid.Source.Target = fvf.Value;
		cid.Target.Target = ivf.Value;
		veqd.TargetValue.Target = ivf.Value;
		veqd.Reference.Value = 1;
		var tsdSource = GetConfigVariableSource<bool>(runner, "TSD.Source", TSDS);
		veqd.Target.Target = tsdSource;

		return tsdSource;
	}

	public static void DriveFromTSD(User runner, IField<bool> output)
	{
		Slot TSDS = GetTimeSineDriverSlot(runner);
		var source = GetConfigVariableSource<bool>(runner, "TSD.Source", TSDS);
		if (source == null) return;
		output.DriveFrom(source);
	}

	// root should be an object that you are ok with being overridden
	public static async Task CloudSpawn(Uri uri, Slot root)
	{
		await root.LoadObjectAsync(uri);
		root.GetComponent<InventoryItem>()?.Unpack();
	}
}
