using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using DV.WeatherSystem;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace DV_NO_NIGHT
{
  public static class Main
  {
    public static bool Enabled;
    public static DateTime? OriginalDateTime;
    public static DateTime? SetDateTime;

    // Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
    public static bool Load(UnityModManager.ModEntry modEntry)
    {
      Harmony? harmony = null;
      modEntry.OnToggle = OnToggle;

      try
      {
        harmony = new Harmony(modEntry.Info.Id);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
      }
      catch (Exception ex)
      {
        modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
        harmony?.UnpatchAll();
        return false;
      }

      return true;
    }

    static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        Enabled = value;
        modEntry.Logger.Log("DV_NO_NIGHT Postfix has been " + (Enabled ? "enabled" : "disabled") + ".");
        return true;
    }
  }

  [HarmonyPatch(typeof(TOD_CycleParameters), nameof(TOD_CycleParameters.DateTime), MethodType.Getter)]
  public class TODDateTimePatch
  {
    public static void Postfix(ref DateTime __result)
    {
      if (Main.Enabled)
      {
        Main.OriginalDateTime ??= __result;
        __result = new DateTime(2000, 01, 1, 12, 0, 0);
        Main.SetDateTime = __result;
      }
      else
      {
        if (!Main.OriginalDateTime.HasValue) return;
        __result = Main.OriginalDateTime.Value;
        Main.SetDateTime = __result;
        Main.OriginalDateTime = null;
      }
    }
  }

  [HarmonyPatch(typeof(TOD_CycleParameters), nameof(TOD_CycleParameters.DateTime), MethodType.Setter)]
  public class TODDateTimeSetterPatch
  {
    public static DateTime? ReleaseTime;
    public static bool MainWasEnabled;
    public static bool Prefix(TOD_CycleParameters __instance)
    {
      var ret = false;
      if (Main.Enabled)
      {
        ReleaseTime = null;
        ret = false;
      }
      else
      {
        if (!ReleaseTime.HasValue && MainWasEnabled)
        {
          ReleaseTime = DateTime.Now.AddSeconds(2);
        }
        if (!ReleaseTime.HasValue || (DateTime.Now - ReleaseTime.Value).TotalSeconds > 0)
        {
          MainWasEnabled = false;
          ret = true;
        }
        else
        {
          ret = false;
        }
      }
      MainWasEnabled = Main.Enabled;
      if (Main.SetDateTime.HasValue)
        __instance.Hour = Main.SetDateTime.Value.Hour;
      return ret;
    }
  }
}