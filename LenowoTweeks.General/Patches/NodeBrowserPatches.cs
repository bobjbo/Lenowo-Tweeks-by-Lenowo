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
    
    [HarmonyPatch(typeof(ComponentSelector), "BuildUI")]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
                codes.Insert(i+1,new(OpCodes.Ldloc,2));
                codes.Insert(i+1,new(OpCodes.Ldfld,find1));
                codes.Insert(i+1,new(OpCodes.Ldloc_0));
                return codes.AsEnumerable();
            }
        }
        return codes.AsEnumerable();
    }
    
    static void AddDefaults(UIBuilder ui, Type baseType)
    {
        List<string> messages = ["Hello, $USER.", "HAIIIIIIIIIIIIIIIIIIII", "Whatcha here for?", "$USER is stinky.", "Lookin' for an illegal type?", "What if it could be SH64?", "<size=700%><color=red>F</color>IH"];
        var msg = RandomX.Range(messages.Count);

        if (msg == 6)
        {
            ui.Text("");
            ui.Text("");
            ui.Text("");
        }

        ui.Text("<size=400%><color=hero.purple>" + messages[msg].Replace("$USER", ui.World.LocalUser.UserName) + "</size>");
        
        if (msg == 6)
        {
            //resdb:///1ed74a85e7d9a8e2456fadbd276cb9dd223ad96377ad230409693d408524606e.wav
            ui.Text("");
            ui.Text("");
            var text = ui.Text("");
            var clip = text.Slot.AttachComponent<StaticAudioClip>();
			clip.URL.Value = new("resdb:///1ed74a85e7d9a8e2456fadbd276cb9dd223ad96377ad230409693d408524606e.wav");
            var speed = RandomX.Range(.5f,2f);
            for (int i = 0; i < 100; i++)
            {
                text.Slot.PlayOneShot(clip,distanceSpace: AudioDistanceSpace.Global,speed: speed).MinDistance.Value = 10f;
            }
        }

        List<Type> DefaultTypes = [typeof(Slot), typeof(User), typeof(IAssetProvider<Material>), typeof(IAssetProvider<ITexture2D>)];
        
        foreach (var type in DefaultTypes)
        {
           
        }

    }

}
