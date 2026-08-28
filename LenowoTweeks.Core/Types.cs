namespace LenowoTweeks.Core;

using ResoniteModLoader;

public interface ILenowoModule
{
	public string ModuleName { get; }
	public string ModuleVersion { get; }

	public ModConfiguration? GetConfig { get; }
	public Dictionary<string, Dictionary<string, List<ModConfigKey>>> GetKeys { get; }
	public int ConfigOrder { get; }
}

public abstract class LenowoTweak : ResoniteMod, ILenowoModule
{
	public abstract string ModuleName { get; }
	public abstract string ModuleVersion { get; }
	public abstract ModConfiguration? GetConfig { get; }
	public abstract Dictionary<string, Dictionary<string, List<ModConfigKey>>> GetKeys { get; }
	public virtual int ConfigOrder => 0;

	public override void OnEngineInit()
	{
		LenowoTweeks_Core.RegisterModule(this);

		Init();
	}


	public virtual void Init() { }
}
