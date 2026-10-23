using UnityEngine;
using Verse;

namespace BNF.Core.DecalSystem
{

    public class DecalKindExtension : DefModExtension
    {
        public string armorDecalPath = "";
        public Color armorDecalColor = new Color(0.2f, 0.2f, 0.2f);

        public string helmetDecalPath = "";
        public Color helmetDecalColor = new Color(0.2f, 0.2f, 0.2f);
        
        public bool overrideSaved = false;
    }
}
