using HarmonyLib;

using ResoniteModLoader;

using System.Reflection;

using FrooxEngine.UIX;

using LenowoTweeks.Core;



#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace LenowoTweeks.General;

public class LenowoTweeks_General : LenowoTweak
{
	private static Assembly ModAssembly => typeof(LenowoTweeks_General).Assembly;
	internal const string VERSION_CONSTANT = "2.0.3";
	const string ModName = "Lenowo Tweeks (General)";

	public override string ModuleName => "General";
	public override string ModuleVersion => VERSION_CONSTANT;

	public override string Name => ModName;
	public override string Author => "Rosa";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/bobjbo/Lenowo-Tweeks-by-Lenowo";
	public const string harmonyID = "com.Lenowo.LenowoTweeks.General";
	static Harmony harmony = new(harmonyID);

	public static ResoniteMod? instance;

	public static ModConfigKey<ValueFieldDropMode> valueFieldDroppingMode = new("Valuefield Dropping Mode", "This controls how ValueFields are allowed to drop into TextFields.\t <size=90%>AlwaysAllow = Works as normal, AllowIfNotSelf = Prevent ValueFields under the same ObjectRoot as the TextField, NeverAllow = Never drop ValueFields</size>", ValueFieldDropMode.AlwaysAllow);
	public static ModConfigKey<string> worldSearchGlobalTags = new("Global World Search Filter", "Global Tags to apply to all world searches.", "", "World Search Global Tags");

	public static ModConfigKey<bool> getItemLink = new("Get Item Link", "If enabled, adds some buttons to the dash for getting a resdb or resrec. Same as the GetItemLink mod, but part of here for rml support", false);

	public static ModConfiguration? Config;

	public static readonly Dictionary<string, Dictionary<string, List<ModConfigKey>>> SortedConfigKeys = new()
	{
		{
			"General", new()
			{
				{ "Base", [ valueFieldDroppingMode, worldSearchGlobalTags, getItemLink ] }
			}
		}
	};

	public override ModConfiguration? GetConfig => instance.GetConfiguration();
	public override Dictionary<string, Dictionary<string, List<ModConfigKey>>> GetKeys => SortedConfigKeys;
	public override int ConfigOrder => -9;

	public static void GenerateUI(UIBuilder ui)
	{
		//new ConfigUIBuilder(instance?.GetConfiguration()).BuildConfigUI(ui, SortedConfigKeys);
	}
	public static void ModSettings_BuildModUi(UIBuilder ui)
	{
		LenowoTweeks_Core.ModSettings_BuildModUi(ui);
	}

	public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
	{
		builder.AutoSave(true);
		foreach (var category in SortedConfigKeys.Values)
		{
			foreach (var configKeys in category.Values)
			{
				foreach (var configKey in configKeys)
				{
					builder.Key(configKey.ConfigKey);
				}
			}
		}
	}

	public override void Init()
	{

#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif

		Config = GetConfiguration();

		Config.Save(true);

		instance = this;

		harmony.PatchAll(ModAssembly);
	}

#if DEBUG
	static void BeforeHotReload()
	{
		harmony.UnpatchAll(harmonyID);
	}

	static void OnHotReload(ResoniteMod modInstance)
	{
		instance = modInstance;
		Config = modInstance.GetConfiguration();
		LenowoTweeks_Core.RegisterModModule(modInstance);
		harmony.PatchAll(ModAssembly);
	}
#endif
}
