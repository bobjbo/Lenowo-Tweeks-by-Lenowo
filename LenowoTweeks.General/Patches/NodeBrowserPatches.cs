using System.Reflection;
using System.Reflection.Emit;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.UIX;

using HarmonyLib;

using LenowoTweeks.Core;

namespace LenowoTweeks.General.Patches;

[HarmonyPatch]
public class NodeBrowserPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ComponentSelector), "BuildUI")]
    public static IEnumerable<CodeInstruction> BeforeBuildGenericTypes(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
		object find1 = null;

        for (int i = 0; i < codes.Count; i++)
        {
			if (codes[i].opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Ldloc_0)
			{
				find1 = codes[i].operand;
			}
            if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand.ToString().Contains("(12)"))
            {
                UniLog.Warning("LENOWO TWEEKS INJECTING DEFAULTS");
                codes.Insert(i+1,new(OpCodes.Call, ((Delegate)AddDefaults).Method));
                codes.Insert(i+1,new(OpCodes.Ldloc_2));
                codes.Insert(i+1,new(OpCodes.Ldfld,find1));
                codes.Insert(i+1,new(OpCodes.Ldloc_0));
                codes.Insert(i+1,new(OpCodes.Ldarg_0));
                return codes.AsEnumerable();
            }
        }
        return codes.AsEnumerable();
    }
    
    static void AddDefaults(ComponentSelector instance, UIBuilder ui, Type baseType)
    {
		AddItemPatches.CurrentTypes.Clear();

        List<string> messages = ["Hello, $USER.", "HAIIIIIIIIIIIIIIIIIIII", "Whatcha here for?", "$USER is stinky.", "Lookin' for an illegal type?", "What if it could be SH64?", "<size=700%><color=red>F</color>IH"];

        var msg = RandomX.Range(messages.Count);

        if (msg == 6)
        {
            var text = ui.Text("").Slot;
            text.AttachComponent<Button>();
            var clip = text.AttachComponent<StaticAudioClip>();
            text.AttachComponent<ButtonAudioClipPlayer>().PressedClips.Add().Clip.Target = clip;
            for (int i = 0; i < 2; i++)
            {
                var text2 = ui.Text("").Slot;
                text2.AttachComponent<Button>();
                var player = text2.AttachComponent<ButtonAudioClipPlayer>();
                player.PressedClips.Add().Clip.Target = clip;
                player.PressedClips[0].MinVolume.Value = 0.1f;
                player.PressedClips[0].MaxVolume.Value = 0.1f;
                player.PressedClips[0].MaxSpeed.Value = 2f;
            }
			clip.URL.Value = new("resdb:///1ed74a85e7d9a8e2456fadbd276cb9dd223ad96377ad230409693d408524606e.wav");
        }

        ui.Text("<size=400%><color=hero.purple>" + messages[msg].Replace("$USER", ui.World.LocalUser.UserName) + "</size>");
        
        if (msg == 6)
        {
            //resdb:///1ed74a85e7d9a8e2456fadbd276cb9dd223ad96377ad230409693d408524606e.wav
            var text = ui.Text("").Slot;
            text.AttachComponent<Button>();
            var clip = text.AttachComponent<StaticAudioClip>();
            text.AttachComponent<ButtonAudioClipPlayer>().PressedClips.Add().Clip.Target = clip;
            for (int i = 0; i < 2; i++)
            {
                var text2 = ui.Text("").Slot;
                text2.AttachComponent<Button>();
                var player = text2.AttachComponent<ButtonAudioClipPlayer>();
                player.PressedClips.Add().Clip.Target = clip;
                player.PressedClips[0].MinVolume.Value = 0.1f;
                player.PressedClips[0].MaxVolume.Value = 0.1f;
                player.PressedClips[0].MaxSpeed.Value = 2f;
            }
			clip.URL.Value = new("resdb:///1ed74a85e7d9a8e2456fadbd276cb9dd223ad96377ad230409693d408524606e.wav");
            var speed = RandomX.Range(.5f,2f);
            for (int i = 0; i < 100; i++)
            {
                text.PlayOneShot(clip,distanceSpace: AudioDistanceSpace.Global,speed: speed).MinDistance.Value = 10f;
            }
        }

        List<Type> DefaultTypes = [typeof(Slot), typeof(User), typeof(IAssetProvider<Material>), typeof(IAssetProvider<ITexture2D>)];
        
        foreach (var type in DefaultTypes)
        {
           AddTypeCustom(instance, ui, type);
        }

    }

	public static void AddTypeCustom(ComponentSelector instance, UIBuilder ui, Type type)
	{
		var button = ui.Button((LocaleString)type.GetNiceName(), new colorX?(RadiantUI_Constants.Sub.CYAN), instance.GetMethodDelegate<ButtonEventHandler<string>>("OnAddComponentPressed"), instance.World.Types.EncodeType(type), 0.35f);
		AddItemPatches.OnAddNewButton(button, instance, ui, type);
	}

	[HarmonyPatch]
	public static class AddItemPatches
	{
		public static HashSet<Type> CurrentTypes = [];
		
		public static MethodInfo? TargetMethod()
		{
			var t1 = typeof(ComponentSelector).InnerTypes().First(t => t.Name == "<>c__DisplayClass16_0");
			return t1.GetMethod("<BuildUI>g__AddGenericTypeButton|0", BindingFlags.NonPublic | BindingFlags.Instance);
		}
		
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = instructions.ToList();

			var getLabelMethod = typeof(Button).GetMethod("get_Label");
			object find1 = null;
			for (int i = 0; i < codes.Count; i++)
			{
				var code = codes[i];
				if (code.opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Ldarg_0)
				{
					find1 = code.operand;
				}
				if (code.Calls(getLabelMethod))
				{
					yield return new(OpCodes.Ldarg_0);
					yield return new(OpCodes.Ldarg_0);
					yield return new(OpCodes.Ldfld, find1);
					yield return new(OpCodes.Ldarg_1);
					yield return new(OpCodes.Call, ((Delegate)OnAddNewButton).Method);
					yield return new(OpCodes.Ret);
					yield break;
				}

				yield return code;
			}
		}

		public static void OnAddNewButton(Button button, ComponentSelector instance, UIBuilder ui, Type innerType)
		{
			if (CurrentTypes.Contains(innerType))
			{
				button.Slot.Destroy();
				return;
			}
			CurrentTypes.Add(innerType);

			// this is default but i remove the rest of the normal code so like, gotta do it here
			button.Label.ParseRichText.Value = false;
		}
	}

}
