using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeDecal : PawnRenderNode
    {
        private readonly DecalSlot _slot;

        public PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            _slot = DetermineSlot(props);
        }
        
        public override Graphic? GraphicFor(Pawn pawn)
        {
            var bnfProps = Props as PawnRenderNodePropertiesOmni;

            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn, _slot);
            string path = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn);
            Color color = profile.Active ? profile.SymbolColor : (bnfProps?.Color ?? new Color(0.2f, 0.2f, 0.2f));

            if (path.NullOrEmpty()) return null;

            if (bnfProps?.autoBodyTypePaths == true && pawn.story?.bodyType != null)
                path = path + "_" + pawn.story.bodyType.defName;

            return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, color);
        }
        
        private static DecalSlot DetermineSlot(PawnRenderNodeProperties props)
        {
            if (props is PawnRenderNodePropertiesOmni bnfProps && bnfProps.ExplicitSlot.HasValue)
                return bnfProps.ExplicitSlot.Value;

            if (props.parentTagDef != null)
            {
                string tagName = props.parentTagDef.defName;
                if (tagName.Contains("Head") || tagName.Contains("Headgear") || tagName.Contains("Helmet"))
                    return DecalSlot.Helmet;
            }

            return DecalSlot.Armor;
        }
        
        private string GetDefaultPath(Pawn pawn)
        {
            if (Props is PawnRenderNodePropertiesOmni bnfProps && bnfProps.texPaths != null && bnfProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return bnfProps.texPaths[seed % bnfProps.texPaths.Count];
            }
            return "";
        }
    }
}
