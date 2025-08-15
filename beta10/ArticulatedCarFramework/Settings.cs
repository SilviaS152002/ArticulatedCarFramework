using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using System.Collections.Generic;
using Model;

namespace NS15
{
	namespace ArticulatedCarFramework
	{
		public static class Main
		{
			public static bool enabled;
			public static Settings settings;

			// TODO add this to settings

			public static Dictionary<string, Type> CarJsonSubTypes = new Dictionary<string, Type>()
			{
				// There was originally something to be done here
				{ "SteamLocomotive", typeof(SteamLocomotive)},
				{"DieselLocomotive", typeof(DieselLocomotive)},
				{"BaseLocomotive", typeof(BaseLocomotive)},
				{"Car", typeof(Car)},
				{"ArticulatedCar", typeof(ArticulatedCar)},
			};

			// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager

			static bool Load(UnityModManager.ModEntry modEntry)
			{
				Harmony? harmony = null;
				settings = Settings.Load<Settings>(modEntry);
				modEntry.OnGUI = OnGUI;
				modEntry.OnSaveGUI = OnSaveGUI;
				modEntry.OnUpdate = OnUpdate;
				modEntry.OnToggle = OnToggle;

				try
				{
					harmony = new Harmony(modEntry.Info.Id);
					harmony.PatchAll(Assembly.GetExecutingAssembly());

					// Other plugin startup logic
				}
				catch (Exception ex)
				{
					modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
					harmony?.UnpatchAll(modEntry.Info.Id);
					return false;
				}

				return true;
			}

			static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
			{

			}

			static void OnGUI(UnityModManager.ModEntry modEntry)
			{
				GUILayout.Toggle(settings.AltCamera, text: "Alternate Camera Behaviour for Articulated Cars");
                GUILayout.Label("      - If one end of an articulated car isn't connected, the camera will instead follow the unconnected end rather than the default location");
                GUILayout.Space(10);
                // settings.Draw(modEntry);
            }

			static void OnSaveGUI(UnityModManager.ModEntry modEntry)
			{
				settings.Save(modEntry);
			}

			static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
			{
				enabled = value;
				return true; // If true, the mod will switch the state. If not, the state will not change.
			}
		}
		public class Settings : UnityModManager.ModSettings, IDrawable
        {
            [Draw("Alternate Camera Behaviour for Articulated Cars")]
            public bool AltCamera = true;
            public override void Save(UnityModManager.ModEntry modEntry)
			{
				Save(this, modEntry);
			}

			public void OnChange()
			{
            }
		}
	}
}
