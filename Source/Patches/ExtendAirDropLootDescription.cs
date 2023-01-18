using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

//###########################################################################
//###########################################################################

public class ExtendAirDropLootDescription
{

    [HarmonyPatch] class ExtendAirDropLootDescriptionPatch
    {

        //#####################################################
        //#####################################################

        static string ExtendAirDropLootDescriptionFn(Entity entity, string desc)
        {
            if (!(entity is EntityAirDrop drop)) return desc;
            if (drop.IsReady == true) return desc;
            return Localization.Get("EntityAirDropWait");
        }

        //#####################################################
        //#####################################################

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PlayerMoveController), "Update");
        }

        //#####################################################
        //#####################################################

        static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldloc_S) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Isinst) continue;
                if (!(codes[i].operand is System.Type type)) continue;
                if (type != typeof(EntityDriveable)) continue;
                if (++i >= codes.Count) break;
                if (codes[i].opcode != OpCodes.Brfalse) continue;
                var pushEntity = codes[i - 2];
                for (; i < codes.Count; i++)
                {
                    if (codes[i].opcode != OpCodes.Ldloc_S) continue;
                    if (!(codes[i].operand is LocalBuilder builder)) continue;
                    if (builder.LocalType != typeof(TileEntityLootContainer)) continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Ldfld) continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Brtrue) continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Ldstr) continue;
                    if (!(codes[i].operand is string str)) continue;
                    if (str != "lootTooltipNew") continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Call) continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Ldloc_S) continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Ldloc_S) continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Call) continue;
                    if (++i >= codes.Count) break;
                    if (codes[i].opcode != OpCodes.Stloc_S) continue;
                    codes.Insert(i + 1, new CodeInstruction(pushEntity));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldloc, codes[i].operand));
                    codes.Insert(i + 3, CodeInstruction.Call(
                        typeof(ExtendAirDropLootDescriptionPatch),
                        "ExtendAirDropLootDescriptionFn"));
                    codes.Insert(i + 4, codes[i]);
                }
            }
            return codes;
        }

        //#####################################################
        //#####################################################

    }
}