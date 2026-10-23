using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem

{
    //BodyType Offsets, because Vanilla does not have this for the actual Rimworld bodytypes, this works
    public class BodyTypeOffsetsByFacingRow
    {
        public BodyTypeDef? BodyType;
        public Vector3 Offset, North, East, South, West;
        public bool HasOffset, HasNorth, HasEast, HasSouth, HasWest;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(BodyType), xmlRoot["bodyType"]?.InnerText);
            ReadVec(xmlRoot, "offset", ref Offset, ref HasOffset);
            ReadVec(xmlRoot, "north", ref North, ref HasNorth);
            ReadVec(xmlRoot, "east", ref East, ref HasEast);
            ReadVec(xmlRoot, "south", ref South, ref HasSouth);
            ReadVec(xmlRoot, "west", ref West, ref HasWest);
        }

        private static void ReadVec(XmlNode root, string nodeName, ref Vector3 vec, ref bool flag)
        {
            var node = root[nodeName];
            if (node == null) return;
            vec = ParseHelper.FromString<Vector3>(node.InnerText);
            flag = true;
        }
    }
}