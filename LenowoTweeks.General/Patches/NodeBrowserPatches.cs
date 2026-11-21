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
	public static bool ShouldBeAParty(ComponentSelector instance) =>
		LenowoTweeks_General.partyMode.Value && (LenowoTweeks_General.shareTheParty.Value || (instance.GetAllocatingUser()?.IsLocalUser ?? false));

	[HarmonyTranspiler]
	[HarmonyPatch(typeof(ComponentSelector), "BuildUI")]
	public static IEnumerable<CodeInstruction> BeforeBuildGenericTypes(IEnumerable<CodeInstruction> instructions)
	{
		bool inject1 = false;
		bool inject2 = false;
		bool inject3 = false;
		bool found1 = false;
		bool found2 = false;
		var codes = new List<CodeInstruction>(instructions);

		var getLabelMethod = typeof(Button).GetMethod("get_Label");
		object find1 = null;

		for (int i = 0; i < codes.Count; i++)
		{
			if (codes[i].opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Ldloc_0)
			{
				find1 = codes[i].operand;
			}
			if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand.ToString().Contains("(12)") && !inject1)
			{
				codes.Insert(i + 1, new(OpCodes.Call, ((Delegate)AddDefaults).Method));
				codes.Insert(i + 1, new(OpCodes.Ldloc_2));
				codes.Insert(i + 1, new(OpCodes.Ldfld, find1));
				codes.Insert(i + 1, new(OpCodes.Ldloc_0));
				codes.Insert(i + 1, new(OpCodes.Ldarg_0));
				inject1 = true;
			}
			else if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString().Contains(" >") && inject1 && !found1)
			{
				found1 = true;
			}
			else if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString().Contains("/") && inject1 && found1)
			{
				found2 = true;
			}
			else if (!inject2 && codes[i].Calls(getLabelMethod) && inject1 && found1 && found2)
			{
				codes[i] = new(OpCodes.Ldarg_0);
				codes[i+1] = new(OpCodes.Ldloc_0);
				codes[i+2] = new(OpCodes.Ldfld, find1);
				codes[i+3] = new(OpCodes.Ldloc, 20);
				codes.Insert(i + 4, new(OpCodes.Call, ((Delegate)Catagory).Method));
				inject2 = true;
			}
			else if (!inject3 && inject1 && inject2 && found1 && found2 && codes[i-1].opcode == OpCodes.Ldloc_S && codes[i-1].operand.ToString().Contains("(25)") && codes[i].Calls(getLabelMethod))
			{
				codes.Insert(i+4, new(OpCodes.Call, ((Delegate)Otherbutton).Method));
				codes.Insert(i+4, new(OpCodes.Ldarg_3)); //group
				codes.Insert(i+4,new(OpCodes.Ldfld,find1)); //ui pt1
				codes.Insert(i+4, new(OpCodes.Ldloc_0)); //ui pt2
				codes.Insert(i+4, new(OpCodes.Ldarg_0)); //component selector
				codes.Insert(i+4, new(OpCodes.Ldloc_S, 25)); // button
				inject3 = true;
			}
		}
		return codes.AsEnumerable();
	}

	public static void Otherbutton(Button button, ComponentSelector instance, UIBuilder ui, string catagory)
	{
		if (ShouldBeAParty(instance))
		{
			button.Label.Color.Value = RandomX.Hue;
			button.Slot.GetComponent<Image>().Tint.Value = RandomX.Hue.MulValue(0.2f);

			if (button.Label.Content.Value.Contains("Fire"))
			{
				button.Label.Content.Value =  button.Label.Content.Value.Replace("Fire"," Fire").Replace("On"," On").Replace("Local"," Local").Replace("True"," Deez").Replace("False"," Dose").Replace("While", " While").Replace("Change"," Nuts Change");
				//var index = button.Label.Content.Value.IndexOf("O");
				//var containsLocal = button.Label.Content.Value.Substring(index).Contains("Local");
				//var containsTrue = button.Label.Content.Value.Substring(index).Contains("True");
				//button.Label.Content.Value = button.Label.Content.Value.Substring(0,index) + " On" + (containsTrue ? " Dose": " Deez") + (containsLocal ? "" : " Local") + " Nuts";
			}
		}
	}

	public static void Catagory(Button button,ComponentSelector instance, UIBuilder ui, CategoryNode<Type> catagory)
	{
		button.Label.ParseRichText.Value = ShouldBeAParty(instance);
		button.Label.Content.Value = catagory.Name + " (" + catagory.ElementCount + (ShouldBeAParty(instance) ? ") <color=hero.purple>:3" : ") >");
		
		if (ShouldBeAParty(instance))
		{
			button.Label.Color.Value = RandomX.Hue;
			button.Slot.GetComponent<Image>().Tint.Value = RandomX.Hue.MulValue(0.2f);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ComponentSelector), "BuildUI")]
	public static void AfterBuildGenericTypes(ComponentSelector __instance)
	{
		// in case of setting for customizable randomized messages, this is out here for that
		var headertext = __instance.Slot.FindChild("Image").FindChild("Header").FindChildInHierarchy("Text").GetComponent<Text>();

		if (ShouldBeAParty(__instance))
		{	
			// its a party!!
			var num = RandomX.Hue;
			__instance.Slot.FindChild("Image").GetComponent<Image>().Tint.Value = num;
			headertext.Color.Value = RandomX.Hue;

			//Horifying Wobbler
			var wobble = __instance.Slot.GetComponentOrAttach<Wobbler2D>(out bool wobblerAttached);
			if (wobblerAttached)
			{
				wobble.Target.Target = __instance.Slot.GetComponent<Canvas>().Size;
				wobble.Offset.Value = new float2(800f, 1200f);
				Traverse.Create(wobble).Field<Sync<float2>>("Speed").Value.Value = new(0.05f, 0.05f);
				Traverse.Create(wobble).Field<Sync<float2>>("Magnitude").Value.Value = new(200f, 200f);
			}

			var AudioClip = __instance.Slot.GetComponentOrAttach<StaticAudioClip>(out bool audioClipAttached);
			if (audioClipAttached)
			{
				List<string> clips = ["resdb:///bdc6be11fdeede104a4b0cb3ea2f583bbe48c6aed97731313a818af08d3609c1", "resdb:///9de8c4e33701c7d74f047c5f4075296ee34e475fdf196fabb2b5a9491a92704c"];
				bool doRandom = RandomX.Chance(0.2f);

				AudioClip.URL.Value = new(doRandom ? clips[RandomX.Range(clips.Count)] : clips[0]);

				var ClipPlayer = __instance.Slot.GetComponentOrAttach<AudioClipPlayer>();
				ClipPlayer.Clip.Target = AudioClip;
				ClipPlayer.Loop = true;
				var Output = __instance.Slot.GetComponentOrAttach<AudioOutput>();
				Output.Source.Target = ClipPlayer;
				Output.DistanceSpace.Value = AudioDistanceSpace.Global;
				Output.DopplerLevel.Value = 0f;
				Output.Volume.Value = 0.1f;
				ClipPlayer.Play();
			}

			List<string> messages = ["OH GREAT HEAVENS", "ruh roh raggy", "oh so colorful", ";3"];

			var msg = RandomX.Range(messages.Count);

			headertext.Content.Value = messages[msg];
		}
	}


	public static void AddDefaults(ComponentSelector instance, UIBuilder ui, Type baseType)
	{
		// this should always run no matter what
		AddItemPatches.CurrentTypes.Clear();

		if (ShouldBeAParty(instance))
		{
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
				// fish!
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
				var speed = RandomX.Range(.5f, 2f);
				for (int i = 0; i < 100; i++)
				{
					text.PlayOneShot(clip, distanceSpace: AudioDistanceSpace.Global, speed: speed).MinDistance.Value = 10f;
				}
			}
		}

		List<Type> DefaultTypes = [typeof(Slot), typeof(User), typeof(IAssetProvider<Material>), typeof(IAssetProvider<ITexture2D>), typeof(IAssetProvider<AudioClip>), typeof(StaticAudioClip), typeof(IWorldAudioDataSource), typeof(string), typeof(IWorldElement), typeof(RefID), typeof(IField<float3>), typeof(IField<float>), typeof(IField<floatQ>), typeof(float3), typeof(floatQ)];

		foreach (var type in DefaultTypes)
		{
			try
			{
				var gt = baseType.MakeGenericType(type);
				if (!type.IsValidGenericType(true) || !instance.World.Types.IsSupported(gt)) continue;
				AddTypeCustom(instance, ui, gt);
			} catch
			{
				// the type is not anywhere near valid. nice one!
			}
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
			// this some evil shit
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
			// already added? dont do it again!
			// - optimally this is done before the button is even created but its not like it matters *that* much
			if (CurrentTypes.Contains(innerType))
			{
				button.Slot.Destroy();
				return;
			}
			CurrentTypes.Add(innerType);

			// this is default but i remove the rest of the normal code so like, gotta do it here
			button.Label.ParseRichText.Value = false;

			// ok now the silly stuff

			//the horrors
			//button.Label.Content.Value = "<size=300%>" + button.Label.Content.Value + "                                  <color=hero.red><size=200%>OwO?";
		}
	}

}
