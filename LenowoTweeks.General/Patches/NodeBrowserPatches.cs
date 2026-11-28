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
	public static bool ShouldBeAParty(IWorldElement instance)
	{
		Slot s = instance.FindNearestParent<Slot>();
		if (IsPartyDenied(s)) return false;
		if (IsAlreadyAParty(s)) return true;
		if (!LenowoTweeks_General.partyMode.Value) return false;
		if (LenowoTweeks_General.shareTheParty.Value) return true;
		if (instance.GetAllocatingUser()?.IsLocalUser ?? false) return true;
		return false;
	}

	public static bool IsPartyDenied(Slot slot) =>
		slot.GetObjectRoot().GetComponent<Comment>(c => c.Text.Value == "ThisIsNotAParty") != null;

	public static bool IsAlreadyAParty(Slot slot) =>
		slot.GetObjectRoot().GetComponent<Comment>(c => c.Text.Value == "ThisIsAParty") != null;

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
				codes[i + 1] = new(OpCodes.Ldloc_0);
				codes[i + 2] = new(OpCodes.Ldfld, find1);
				codes[i + 3] = new(OpCodes.Ldloc, 20);
				codes.Insert(i + 4, new(OpCodes.Call, ((Delegate)Catagory).Method));
				inject2 = true;
			}
			else if (!inject3 && inject1 && inject2 && found1 && found2 && codes[i - 1].opcode == OpCodes.Ldloc_S && codes[i - 1].operand.ToString().Contains("(25)") && codes[i].Calls(getLabelMethod))
			{
				codes.Insert(i + 4, new(OpCodes.Call, ((Delegate)Otherbutton).Method));
				codes.Insert(i + 4, new(OpCodes.Ldarg_3)); //group
				codes.Insert(i + 4, new(OpCodes.Ldfld, find1)); //ui pt1
				codes.Insert(i + 4, new(OpCodes.Ldloc_0)); //ui pt2
				codes.Insert(i + 4, new(OpCodes.Ldarg_0)); //component selector
				codes.Insert(i + 4, new(OpCodes.Ldloc_S, 25)); // button
				inject3 = true;
			}
		}
		return codes.AsEnumerable();
	}

	public static void Otherbutton(Button button, ComponentSelector instance, UIBuilder ui, string catagory)
	{
		if (ShouldBeAParty(instance))
		{
			if (button.Label.Content.Value.Contains("Fire"))
			{
				button.Label.Content.Value = button.Label.Content.Value.Replace("True", "Deez").Replace("False", "Dose").Replace("Change", "NutsChange");
				//var index = button.Label.Content.Value.IndexOf("O");
				//var containsLocal = button.Label.Content.Value.Substring(index).Contains("Local");
				//var containsTrue = button.Label.Content.Value.Substring(index).Contains("True");
				//button.Label.Content.Value = button.Label.Content.Value.Substring(0,index) + " On" + (containsTrue ? " Dose": " Deez") + (containsLocal ? "" : " Local") + " Nuts";
			}
			if (button.Label.Content.Value.Contains("Async"))
			{
				button.Label.Content.Value = button.Label.Content.Value.Replace("Async", "ASink");
			}
		}


		ModifyButton(button, true);


	}

	public static void Catagory(Button button, ComponentSelector instance, UIBuilder ui, CategoryNode<Type> catagory)
	{
		button.Label.ParseRichText.Value = ShouldBeAParty(instance);
		if (LenowoTweeks_General.showCategoryCounts.Value)
		{	
			button.Label.Content.Value = catagory.Name + " (" + catagory.ElementCount + (ShouldBeAParty(instance) ? ") <color=hero.purple>:3" : ") >");
		}


		ModifyButton(button, false);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ComponentSelector), "BuildUI")]
	public static void AfterBuildGenericTypes(ComponentSelector __instance)
	{
		var slot = __instance.Slot;
		// in case of setting for customizable randomized messages, this is out here for that
		var headertext = slot.FindChild("Image").FindChild("Header").FindChildInHierarchy("Text").GetComponent<Text>();

		if (LenowoTweeks_General.noParty.Value)
		{
			if (__instance.GetAllocatingUser()?.IsLocalUser ?? false)
			{
				if (slot.GetComponent<Comment>(c => c.Text.Value == "ThisIsNotAParty") == null)
				{
					slot.AttachComponent<Comment>().Text.Value = "ThisIsNotAParty";
				}
			}
		}

		if (ShouldBeAParty(__instance))
		{
			if (slot.GetComponent<Comment>(c => c.Text.Value == "ThisIsAParty") == null)
			{
				slot.AttachComponent<Comment>().Text.Value = "ThisIsAParty";
			}
			// its a party!!
			var num = RandomX.Hue;
			slot.FindChild("Image").GetComponent<Image>().Tint.Value = num;
			headertext.Color.Value = RandomX.Hue;

			//Horifying Wobbler
			var wobble = slot.GetComponentOrAttach<Wobbler2D>(out bool wobblerAttached);
			if (wobblerAttached)
			{
				wobble.Target.Target = slot.GetComponent<Canvas>().Size;
				wobble.Offset.Value = new float2(800f, 1200f);
				Traverse.Create(wobble).Field<Sync<float2>>("Speed").Value.Value = new(0.05f, 0.05f);
				Traverse.Create(wobble).Field<Sync<float2>>("Magnitude").Value.Value = new(200f, 200f);
			}

			var AudioClip = slot.GetComponentOrAttach<StaticAudioClip>(out bool audioClipAttached);
			if (audioClipAttached)
			{
				List<string> clips = ["resdb:///bdc6be11fdeede104a4b0cb3ea2f583bbe48c6aed97731313a818af08d3609c1", "resdb:///9de8c4e33701c7d74f047c5f4075296ee34e475fdf196fabb2b5a9491a92704c", "resdb:///bacee42662807e1bec7bf33f20b211bcbd05b7584be81787817c2f3af44a3130.ogg", "resdb:///bff1f05cd334eee789951cdbcf05c937486db23cb9279f04a27cae8d583e1b9e.ogg", "resdb:///994288f0f8348673bf427ff5234a61f7db93e8392f16a3b0871217506e7fa2ad", "resdb:///fe5cc5988d68caeb4c67d112115cdda4ffaeece9ba0a0d9033134a8523279f9e.wav"];
				bool doRandom = RandomX.Chance(0.5f);

				AudioClip.URL.Value = new(doRandom ? clips[RandomX.Range(clips.Count)] : clips[0]);

				var ClipPlayer = slot.GetComponentOrAttach<AudioClipPlayer>();
				ClipPlayer.Clip.Target = AudioClip;
				ClipPlayer.Loop = true;
				var Output = slot.GetComponentOrAttach<AudioOutput>();
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
		else
		{
			var bg = slot.FindChild("Image").GetComponent<Image>();
			if (LenowoTweeks_General.backgroundColorOverride.Value != new colorX(0f, 0f))
			{
				bg.Tint.Value = LenowoTweeks_General.backgroundColorOverride.Value;
			}
			else if (LenowoTweeks_General.changeBGColor.Value)
			{
				bg.Tint.Value = LenowoTweeks_Core.secondaryUIColor.Value;
			}
		}
	}

	public static List<Type> FavTypes = [
		typeof(Slot), typeof(User),
		typeof(IAssetProvider<Material>), typeof(IAssetProvider<ITexture2D>), typeof(IAssetProvider<AudioClip>), typeof(StaticAudioClip), typeof(IWorldAudioDataSource),
		typeof(string), typeof(IWorldElement), typeof(RefID),
		typeof(IField<float3>), typeof(IField<float>), typeof(IField<floatQ>),
		typeof(float3), typeof(floatQ)
	];

	public static void AddDefaults(ComponentSelector instance, UIBuilder ui, Type baseType)
	{
		// this should always run no matter what
		AddItemPatches.CurrentTypes.Clear();

		if (LenowoTweeks_General.attacherMessages.Value)
		{
			var world = ui.World;

			List<string> messages = [
				"Hello, $USER.", "HAIIIIIIIIIIIIIIIIIIII", "Whatcha here for?", "Lookin' for an illegal type?", "What if it could be SH64?",
				// if alone, twice the chance of calling the user stinky :D
				"$USER is stinky.", "$RANDOMUSER is stinky."
			];
			List<string> aloneMessages = ["Where did everyone go?", "It's quiet here.", "Maybe join another session?"];
			List<string> otherUserMessages = ["$HOST is watching.", "Where did $RANDOMUSER go?"];
			List<string> partyMessages = ["<size=700%><color=red>F</color>IH"];

			var partyMsg = ShouldBeAParty(instance) ? partyMessages : [];
			var userCountBasedMsgs = world.ActiveUserCount == 1 ? aloneMessages : otherUserMessages;

			var msgs = messages.Concat(userCountBasedMsgs).Concat(partyMsg).ToList();

			var msg = RandomX.Range(msgs.Count);

			bool fish = msgs[msg] == partyMessages[0];
			if (fish)
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
					var clipData = player.PressedClips.Add();
					clipData.Clip.Target = clip;
					clipData.MinVolume.Value = 0.1f;
					clipData.MaxVolume.Value = 0.1f;
					clipData.MaxSpeed.Value = 2f;
				}
				clip.URL.Value = new("resdb:///1ed74a85e7d9a8e2456fadbd276cb9dd223ad96377ad230409693d408524606e.wav");
			}

			var randomUser = ui.World.AllUsers.ToList().GetRandom();

			var realMsg = msgs[msg]
				.Replace("$USER", ui.World.LocalUser.UserName)
				.Replace("$HOST", ui.World.HostUser.UserName)
				.Replace("$RANDOMUSER", randomUser.UserName);

			ui.Text($"<size=400%><color=hero.purple>{realMsg}</size>");

			if (fish)
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
					var clipData = player.PressedClips.Add();
					clipData.Clip.Target = clip;
					clipData.MinVolume.Value = 0.1f;
					clipData.MaxVolume.Value = 0.1f;
					clipData.MaxSpeed.Value = 2f;
				}
				clip.URL.Value = new("resdb:///1ed74a85e7d9a8e2456fadbd276cb9dd223ad96377ad230409693d408524606e.wav");
				var speed = RandomX.Range(.5f, 2f);
				for (int i = 0; i < 15; i++)
				{
					text.PlayOneShot(clip, distanceSpace: AudioDistanceSpace.Global, speed: speed).MinDistance.Value = 10f;
				}
			}

			// todo:
			// resdb:///68c34cbf6375b257ca4175412b6f8a9033af0e39c74cf78485488513b689fbdf
			// resdb:///7d57871b41c93e4e83d597af1dd723d81adef3f7579ccc025ed563d4383bf28e
			// resdb:///e4195228e77008e6c4dfe887d973080bd8c5c7f480373917789acdf3a64f64a4.wav

		}

		if (LenowoTweeks_General.addDefaultTypes.Value)
		{
			var favButton = ui.Button("*");
			var favSlot = favButton.Slot;
			Slot pickerSlot = favSlot.Parent.Children.LastOrDefault(s=>s.Name == "Panel");
			
			pickerSlot.Children[pickerSlot.ChildrenCount - 1].GetComponent<RectTransform>().AnchorMax.Value = new(0.94f, 1f);
			favSlot.Parent = pickerSlot;
			favSlot.GetComponent<RectTransform>().AnchorMin.Value = new(0.945f, 0f);

			foreach (var type in FavTypes)
			{
				try
				{
					var gt = baseType.MakeGenericType(type);
					if (!gt.IsValidGenericType(true) || !instance.World.Types.IsSupported(gt)) continue;
					AddTypeCustom(instance, ui, gt);
				} catch
				{
					// the type is not anywhere near valid. nice one!
				}
			}
		}

	}

	public static void AddTypeCustom(ComponentSelector instance, UIBuilder ui, Type type)
	{
		var button = ui.Button((LocaleString)type.GetNiceName(), new colorX?(RadiantUI_Constants.Sub.CYAN), instance.GetMethodDelegate<ButtonEventHandler<string>>("OnAddComponentPressed"), instance.World.Types.EncodeType(type), 0.35f);
		AddItemPatches.OnAddNewButton(button, instance, ui, type);
	}

	public static void ModifyButton(Button button, bool dospaces)
	{
		bool globalDoSpaces = LenowoTweeks_General.addSpacesInNames.Value;

		var label = button.Label;
		var tint = button.Slot.GetComponent<Image>().Tint;

		if (globalDoSpaces && dospaces)
		{
			var content = label.Content;
			content.Value = content.Value.Replace("_", " ");

			bool lastUpper = false;
			for (int i = 0; i < content.Value.Length && i < 100; i++)
			{
				var cur = content.Value[i];
				if (cur == '<')
				{
					content.Value = content.Value.Insert(i, " ");
					break;
				}
				
				if (char.IsUpper(cur) && !char.IsDigit(cur))
				{
					bool temp = lastUpper;
					lastUpper = true;
					if (i == 0) continue;
					if (i > 3 && content.Value.Substring(i - 2, 3) == "NaN") continue;
					if (!temp)
					{
						content.Value = content.Value.Insert(i, " ");
						i++;
					}
					continue;
				}

				lastUpper = false;
			}

			// prevent double/triple spaces with this one simple trick!
			content.Value = content.Value.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
		}

		if (ShouldBeAParty(button.Slot))
		{
			label.Color.Value = RandomX.Hue;
			button.Slot.GetComponent<Image>().Tint.Value = RandomX.Hue.MulValue(0.2f);
		}

		else
		{
			
			if (tint.Value == RadiantUI_Constants.Sub.PURPLE)
			{
				tint.Value = LenowoTweeks_General.groupingColor.Value;
			}
			else if (tint.Value == RadiantUI_Constants.Sub.CYAN)
			{
				tint.Value = LenowoTweeks_General.normalColor.Value;
			}
			else if (tint.Value == RadiantUI_Constants.Sub.GREEN)
			{
				tint.Value = LenowoTweeks_General.genericFolderColor.Value;
			}
			else if (tint.Value == RadiantUI_Constants.Sub.YELLOW)
			{
				tint.Value = LenowoTweeks_General.categoryColor.Value;
			}
		}

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
			bool found = false;
			for (int i = 0; i < codes.Count; i++)
			{
				var code = codes[i];
				if (!found && code.opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Ldarg_0)
				{
					find1 = code.operand;
					found = true;
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

			try
			{
				if (LenowoTweeks_General.addDefaultTypes.Value)
				{
					if (innerType.GenericTypeArguments.Length == 1)
					{
						var genericArg = innerType.GenericTypeArguments[0];
						var buttonHolder = button.Slot.Parent.AddSlot($"{button.LabelText}");
						var bhrt = buttonHolder.AttachComponent<RectTransform>();
						var layout = buttonHolder.AttachComponent<LayoutElement>();
						var hl = buttonHolder.AttachComponent<HorizontalLayout>();
						hl.ForceExpandWidth.Value = false;
						layout.MinHeight.Value = 32;
						hl.Spacing.Value = 4;
						button.Slot.Parent = buttonHolder;
						button.Slot.GetComponent<LayoutElement>().FlexibleWidth.Value = 1;

						ui.NestInto(bhrt);
						var fav = ui.Button(new Uri("resdb:///44e8c56596b266e0099b0f6f9f014dc8929b0ea4eef16fcaa2faba8a079a4e71.png"));
						fav.Slot.Name = "FAVIOURBNOTYBESAOG H";
						var tex1 = fav.Icon.Slot.GetComponent<StaticTexture2D>();
						var tex2 = fav.Icon.Slot.AttachComponent<StaticTexture2D>();
						tex2.URL.Value = new("resdb:///76b194bd2a8bd4c57ea5fbf1b35bf179a23945f13ed723162d2268a43c63a605.png");
						var bad = fav.Icon.Slot.AttachComponent<BooleanAssetDriver<ITexture2D>>();
						bad.FalseTarget.Target = tex1;
						bad.TrueTarget.Target = tex2;
						bad.Target.Target = fav.Icon.Slot.GetComponent<SpriteProvider>().Texture;
						var favl = fav.Slot.GetComponentOrAttach<LayoutElement>();
						favl.MinWidth.Value = 32;
						ui.NestOut();

						Slot buttonHolderHolder = buttonHolder.Parent.FindChildOrAdd("TypeHolder");
						buttonHolderHolder.GetComponentOrAttach<RectTransform>();
						var vl = buttonHolderHolder.GetComponentOrAttach<VerticalLayout>();
						vl.ForceExpandHeight.Value = false;
						vl.Spacing.Value = 8;
						buttonHolder.Parent = buttonHolderHolder;
						
						buttonHolder.OrderOffset = buttonHolderHolder.ChildrenCount - 1;

						if (FavTypes.Contains(genericArg))
						{
							bad.State.Value = true;
							buttonHolder.OrderOffset = -1000000;
						}


						fav.LocalPressed += (_, _) =>
						{
							if (FavTypes.Contains(genericArg))
							{
								FavTypes.Remove(genericArg);
								bad.State.Value = false;
								buttonHolder.OrderOffset += 1000000;
							}
							else
							{
								FavTypes.Add(genericArg);
								bad.State.Value = true;
								buttonHolder.OrderOffset -= 1000000;
							}
						};
					}
				}
			}
			catch (Exception e)
			{
				UniLog.Error($"EPIC!!!!\n{e}");
			}


			ModifyButton(button, true);
		}
	}

}
