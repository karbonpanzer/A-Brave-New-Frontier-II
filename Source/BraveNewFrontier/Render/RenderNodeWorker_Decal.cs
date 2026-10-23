using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerOmniBodyApparel : PawnRenderNodeWorker
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);

            if (parms.pawn == null || !(node.Props is PawnRenderNodePropertiesOmni bnfProps))
                return result;

            var bodyType = parms.pawn.story?.bodyType;
            if (bodyType == null) return result;

            if (bnfProps.BodyTypeOffsetsByFacing.TryGetValue(parms.facing, out var facingMap) &&
                facingMap.TryGetValue(bodyType, out var facingOffset))
            {
                result += facingOffset;
            }
            else if (bnfProps.BodyTypeOffsets.TryGetValue(bodyType, out var globalOffset))
            {
                result += globalOffset;
            }

            return result;
        }

        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;
            if (!parms.flags.FlagSet(PawnRenderFlags.Clothes)) return false;
            return DecalUtil.PawnHasAnyDecalApparel(parms.pawn);
        }
    }
    
    public class PawnRenderNodeWorkerHeadware : PawnRenderNodeWorkerOmniBodyApparel { }
}
