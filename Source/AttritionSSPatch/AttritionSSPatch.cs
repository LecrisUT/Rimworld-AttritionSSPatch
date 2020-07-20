using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace AttritionSSPatch
{
    public sealed class AttritionSSPatch : Mod
    {
        public AttritionSSPatch(ModContentPack content) : base(content)
        {
            // Early patch to avoid generic Def issues
            var harmony = new Harmony("AttritionSSPatch");
            if (ModsConfig.IsActive("petetimessix.simplesidearms"))
            {
                var findBestRangedWeapon = AccessTools.Method("SimpleSidearms.utilities.GettersFilters:findBestRangedWeapon");
                var getCarriedWeapons = AccessTools.Method("SimpleSidearms.Extensions:getCarriedWeapons");
                if (findBestRangedWeapon != null && getCarriedWeapons != null)
                {
                    Harmony.ReversePatch(getCarriedWeapons, new HarmonyMethod(AccessTools.Method(typeof(Patch), "getCarriedRangedWeapon")),
                        AccessTools.Method(typeof(Patch), "ReverseTranspiler"));
                    harmony.Patch(findBestRangedWeapon, transpiler: new HarmonyMethod(AccessTools.Method(typeof(Patch), "Transpiler")));
                }
            }
        }
    }
    public static class Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            var getCarriedWeapons = AccessTools.Method("SimpleSidearms.Extensions:getCarriedWeapons");
            var getCarriedRangedWeapon = AccessTools.Method(typeof(Patch), "getCarriedRangedWeapon");
            return instructions.MethodReplacer(getCarriedWeapons, getCarriedRangedWeapon);
        }
        public static IEnumerable<ThingWithComps> getCarriedRangedWeapon(this Pawn pawn, bool includeEquipped = true, bool includeTools = false)
        {
            throw new NotImplementedException("Stub");
        }
        public static IEnumerable<ThingWithComps> FilterRangedWeapon(IEnumerable<ThingWithComps> things, Pawn pawn)
        {
            foreach (var thing in things)
                if (thing.def.IsRangedWeapon && AvailableRangedWeapon(thing, pawn))
                    yield return thing;
        }
        public static IEnumerable<CodeInstruction> ReverseTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();
            instructionList.InsertRange(instructionList.Count - 1, new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0, null),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch),"FilterRangedWeapon"))
                });
            return instructionList;
        }
        public static bool AvailableRangedWeapon(Thing weapon, Pawn pawn)
        {
            string ammo = Attrition.Utilities.Utility.GetWeaponAmmo(weapon);
            if (ammo == null || ammo == "none")
                return true;
            Thing thing = pawn.inventory.innerContainer.FirstOrDefault(t => t.def.defName.ToLower() == ammo.ToLower());
            return thing != null;
        }
    }
}
