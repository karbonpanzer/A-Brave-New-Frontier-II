using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public sealed class CompEditDecalMarker : ThingComp
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;
        
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "bnfDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "bnfDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "bnfDecalHelmetColor", Color.white);

            Scribe_Values.Look(ref ProfileSet.Armor.Active, "bnfDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "bnfDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "bnfDecalArmorColor", Color.white);
        }
        
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            WorldComponentDecalPawns.Instance?.Register(pawn);
            TryApplyKindDefaults(pawn);
        }
        
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (wornApparel[i].def.HasComp<CompEditDecalMarker>())
                    return;
            }
            WorldComponentDecalPawns.Instance?.Unregister(pawn);
        }
        
        private void TryApplyKindDefaults(Pawn pawn)
        {
            if (pawn.kindDef == null) return;

            var ext = pawn.kindDef.GetModExtension<DecalKindExtension>();
            if (ext == null) return;

            bool armorEmpty = ProfileSet.Armor.SymbolPath.NullOrEmpty();
            bool helmetEmpty = ProfileSet.Helmet.SymbolPath.NullOrEmpty();

            if (!ext.armorDecalPath.NullOrEmpty() && (armorEmpty || ext.overrideSaved))
            {
                ProfileSet.Armor.Active = true;
                ProfileSet.Armor.SymbolPath = ext.armorDecalPath;
                ProfileSet.Armor.SymbolColor = ext.armorDecalColor;
            }

            if (!ext.helmetDecalPath.NullOrEmpty() && (helmetEmpty || ext.overrideSaved))
            {
                ProfileSet.Helmet.Active = true;
                ProfileSet.Helmet.SymbolPath = ext.helmetDecalPath;
                ProfileSet.Helmet.SymbolColor = ext.helmetDecalColor;
            }
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}
