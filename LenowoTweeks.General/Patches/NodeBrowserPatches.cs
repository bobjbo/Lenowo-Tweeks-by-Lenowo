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
        //UniLog.Warning(ui.ToString());
        ui.Text("LenowoTweeks: Added default components for " + baseType.Name);
    }

}
