using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{
    public class PawnRenderNodePropertiesOmni : PawnRenderNodeProperties
    {
        public Color Color = new Color(0.2f, 0.2f, 0.2f);

        public DecalSlot? ExplicitSlot = null;
        
        public bool autoBodyTypePaths = false;
        
        public readonly Dictionary<BodyTypeDef, Vector3> BodyTypeOffsets = new Dictionary<BodyTypeDef, Vector3>();
        public readonly Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>> BodyTypeOffsetsByFacing = new Dictionary<Rot4, Dictionary<BodyTypeDef, Vector3>>();
        public readonly List<BodyTypeOffsetsByFacingRow> BodyTypeOffsetsByFacingRows = new List<BodyTypeOffsetsByFacingRow>();

        public PawnRenderNodePropertiesOmni()
        {
            nodeClass = typeof(PawnRenderNodeDecal);
            workerClass = typeof(PawnRenderNodeWorkerOmniBodyApparel);
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            BuildOffsets();
        }
        
        private void BuildOffsets()
        {
            if (BodyTypeOffsetsByFacingRows == null) return;

            BodyTypeOffsets.Clear();
            BodyTypeOffsetsByFacing.Clear();

            foreach (var row in BodyTypeOffsetsByFacingRows)
            {
                if (row.BodyType == null) continue;

                if (row.HasNorth) AddToFacingDict(Rot4.North, row.BodyType, row.North);
                if (row.HasEast) AddToFacingDict(Rot4.East, row.BodyType, row.East);
                if (row.HasSouth) AddToFacingDict(Rot4.South, row.BodyType, row.South);
                if (row.HasWest) AddToFacingDict(Rot4.West, row.BodyType, row.West);

                if (row.HasOffset)
                    BodyTypeOffsets[row.BodyType] = row.Offset;
                else if (row.HasSouth)
                    BodyTypeOffsets[row.BodyType] = row.South;
            }
        }

        private void AddToFacingDict(Rot4 rot, BodyTypeDef bodyType, Vector3 offset)
        {
            if (!BodyTypeOffsetsByFacing.TryGetValue(rot, out var dict))
            {
                dict = new Dictionary<BodyTypeDef, Vector3>();
                BodyTypeOffsetsByFacing[rot] = dict;
            }
            dict[bodyType] = offset;
        }
    }
}
