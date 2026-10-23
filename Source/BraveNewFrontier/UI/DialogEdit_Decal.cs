using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BNF.Core.DecalSystem
{
    // Trying my best to actually have good documentation for my materials for future references in my other projects.
    public sealed class DialogEditDecals : Window
    {
        private readonly Pawn _pawn;
        private DecalProfileSet _profileSet;
        private readonly DecalProfileSet _original;
        private readonly List<DecalSymbolDef> _symbols;
        private int _selectedHelmetIndex, _selectedArmorIndex;
        private bool _committed;
        private List<Color>? _allColors;

        // Per-slot scroll positions for symbol grids
        private Vector2 _armorGridScroll, _helmetGridScroll;

        // Texture cache for symbol thumbnails
        private Dictionary<string, Texture2D>? _thumbCache;

        // Which tab we're looking at right now
        private DecalSlot _activeTab = DecalSlot.Armor;
        private List<TabRecord>? _tabs;

        // Layout
        private const float GridHeight = 260f;
        private const float TileSize = 48f;
        private const float TilePad = 4f;
        private const float InnerPad = 8f;

        public override Vector2 InitialSize => new Vector2(560f, 780f);

        public DialogEditDecals(Pawn pawn)
        {
            _pawn = pawn;
            forcePause = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;
            preventCameraMotion = false;
            doCloseX = true;
            _profileSet = DecalUtil.ReadProfileSetFrom(_pawn);
            _original = _profileSet;
            _symbols = DecalUtil.AllSymbols();
            _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
            _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
            SyncSelection();
            DecalUtil.SetLiveEditFull(_pawn, _profileSet);
        }

        public override void Close(bool doCloseSound = true)
        {
            DecalUtil.EndLiveEdit(_pawn, _committed, _original);
            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_pawn.Destroyed) { Close(false); return; }

            float pad = 14f;
            float curY = inRect.y;
            float contentW = inRect.width - pad * 2f;
            float contentX = inRect.x + pad;

            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(contentX, curY, contentW, 35f);
            Widgets.Label(titleRect, "BNF_StyleDecalsTitle".Translate(_pawn.Name.ToStringShort));
            Text.Font = GameFont.Small;
            curY = titleRect.yMax + 10f;

            // Tab content area — DrawTabs renders above this rect
            Rect tabContentRect = new Rect(contentX, curY + 32f, contentW, inRect.yMax - curY - 32f - 56f);
            Widgets.DrawMenuSection(tabContentRect);

            // Tabs auto-size to label width, not much we can do about that
            _tabs ??= new List<TabRecord>
            {
                new TabRecord("BNF_Decals_Armor".Translate(), () => _activeTab = DecalSlot.Armor, _activeTab == DecalSlot.Armor),
                new TabRecord("BNF_Decals_Helmet".Translate(), () => _activeTab = DecalSlot.Helmet, _activeTab == DecalSlot.Helmet)
            };
            _tabs[0].selected = (_activeTab == DecalSlot.Armor);
            _tabs[1].selected = (_activeTab == DecalSlot.Helmet);
            TabDrawer.DrawTabs(tabContentRect, _tabs);

            // Draw whichever slot is active
            Rect innerRect = tabContentRect.ContractedBy(InnerPad);
            DrawSlotSection(innerRect, _activeTab);

            // Footer
            DrawBottomButtons(new Rect(contentX, inRect.yMax - 50f, contentW, 44f));
        }

        // Everything stacked vertically: enable row, symbol grid, then colors underneath
        private void DrawSlotSection(Rect rect, DecalSlot slot)
        {
            bool isArmor = (slot == DecalSlot.Armor);
            float curY = rect.y;

            // Enable checkbox + Random button
            Rect enableRow = new Rect(rect.x, curY, rect.width, 24f);
            bool active = isArmor ? _profileSet.Armor.Active : _profileSet.Helmet.Active;
            bool wasActive = active;
            Widgets.CheckboxLabeled(new Rect(enableRow.x, enableRow.y, 160f, enableRow.height),
                "Enabled".Translate(), ref active);
            if (wasActive != active)
            {
                if (isArmor) _profileSet.Armor.Active = active;
                else _profileSet.Helmet.Active = active;
                PushLive();
            }
            if (Widgets.ButtonText(new Rect(enableRow.xMax - 130f, enableRow.y, 130f, enableRow.height),
                "BNF_Decals_RandomSymbol".Translate()) && _symbols.Count > 0)
            {
                if (isArmor) _selectedArmorIndex = Rand.Range(0, _symbols.Count);
                else _selectedHelmetIndex = Rand.Range(0, _symbols.Count);
                SyncSelection();
                PushLive();
            }
            curY = enableRow.yMax + 6f;

            // Symbol grid — fills full width, scroll only inside
            Rect gridRect = new Rect(rect.x, curY, rect.width, GridHeight);
            DrawSymbolGrid(gridRect, slot);
            curY = gridRect.yMax + 26f;

            // Color picker fills the remaining width and height
            float colorH = rect.yMax - curY;
            Rect colorRect = new Rect(rect.x, curY, rect.width, colorH);
            DrawColorSection(colorRect, slot);
        }

        // Scrollable symbol grid a la the vanilla hair menu — only eats scrollbar width when it actually needs to scroll
        private void DrawSymbolGrid(Rect outerRect, DecalSlot slot)
        {
            bool isArmor = (slot == DecalSlot.Armor);
            int selectedIdx = isArmor ? _selectedArmorIndex : _selectedHelmetIndex;
            Vector2 scrollPos = isArmor ? _armorGridScroll : _helmetGridScroll;

            Widgets.DrawMenuSection(outerRect);
            Rect innerRect = outerRect.ContractedBy(4f);

            // Two-pass layout: figure out if we need a scrollbar before committing to column count
            float fullW = innerRect.width;
            int colsFull = Mathf.Max(1, Mathf.FloorToInt((fullW + TilePad) / (TileSize + TilePad)));
            int rowsFull = (_symbols.Count > 0) ? Mathf.CeilToInt((float)_symbols.Count / colsFull) : 1;
            float gridHFull = rowsFull * (TileSize + TilePad) - TilePad;
            bool needsScroll = gridHFull > innerRect.height;

            float usableW = needsScroll ? innerRect.width - 16f : innerRect.width;
            int columns = Mathf.Max(1, Mathf.FloorToInt((usableW + TilePad) / (TileSize + TilePad)));
            int rowCount = (_symbols.Count > 0) ? Mathf.CeilToInt((float)_symbols.Count / columns) : 1;
            float gridH = rowCount * (TileSize + TilePad) - TilePad;

            // Spread tiles across full width so there's no dead space on the right
            float strideX = (columns > 1) ? (usableW - TileSize) / (columns - 1) : 0f;
            float strideY = TileSize + TilePad;

            Rect viewRect = new Rect(0f, 0f, usableW, gridH);
            Widgets.BeginScrollView(innerRect, ref scrollPos, viewRect);

            if (isArmor) _armorGridScroll = scrollPos;
            else _helmetGridScroll = scrollPos;

            for (int i = 0; i < _symbols.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float x = col * strideX;
                float y = row * strideY;
                Rect tileRect = new Rect(x, y, TileSize, TileSize);

                bool isSelected = (i == selectedIdx);

                Widgets.DrawBoxSolid(tileRect, new Color(0.12f, 0.12f, 0.12f, 0.6f));

                // Double highlight so selected tiles actually stand out
                if (isSelected)
                {
                    Widgets.DrawHighlight(tileRect);
                    Widgets.DrawHighlight(tileRect);
                }
                else if (Mouse.IsOver(tileRect))
                {
                    Widgets.DrawHighlight(tileRect);
                }

                Texture2D? thumb = GetThumb(_symbols[i]);
                if (thumb != null)
                {
                    Rect iconRect = tileRect.ContractedBy(3f);
                    GUI.DrawTexture(iconRect, thumb, ScaleMode.ScaleToFit);
                }

                if (Widgets.ButtonInvisible(tileRect))
                {
                    if (isArmor) _selectedArmorIndex = i;
                    else _selectedHelmetIndex = i;
                    SyncSelection();
                    PushLive();
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }

                if (Mouse.IsOver(tileRect))
                    TooltipHandler.TipRegion(tileRect, _symbols[i].LabelCap);
            }

            Widgets.EndScrollView();

            // Selected symbol name below the grid
            Rect labelRect = new Rect(outerRect.x, outerRect.yMax + 2f, outerRect.width, 22f);
            string symName = (_symbols.Count > 0 && selectedIdx >= 0 && selectedIdx < _symbols.Count)
                ? _symbols[selectedIdx].LabelCap.ToString() : "-";
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, symName);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // Color picker and shortcut buttons — fills full width
        private void DrawColorSection(Rect rect, DecalSlot slot)
        {
            bool isArmor = (slot == DecalSlot.Armor);
            float curY = rect.y;

            Widgets.Label(new Rect(rect.x, curY, rect.width, 22f), "BNF_Decals_Color".Translate());
            curY += 26f;

            Color color = isArmor ? _profileSet.Armor.SymbolColor : _profileSet.Helmet.SymbolColor;
            Color original = color;

            // Color selector fills the full width of the panel
            Widgets.ColorSelector(new Rect(rect.x, curY, rect.width, 1000f),
                ref color, AllColors(), out float usedHeight, null, 22, 2);
            curY += usedHeight + 10f;

            // Three shortcut buttons in a single row spanning full width
            float btnH = 28f;
            float gap = 8f;
            float btnW = (rect.width - gap * 2f) / 3f;

            if (Widgets.ButtonText(new Rect(rect.x, curY, btnW, btnH), "BNF_Decals_IdeoColor".Translate()))
            {
                if (TryGetIdeoColor(_pawn, out Color c))
                {
                    color = c;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }
            if (Widgets.ButtonText(new Rect(rect.x + btnW + gap, curY, btnW, btnH), "BNF_Decals_FavColor".Translate()))
            {
                if (TryGetFavoriteColor(_pawn, out Color c))
                {
                    color = c;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }
            if (Widgets.ButtonText(new Rect(rect.x + (btnW + gap) * 2f, curY, btnW, btnH), "BNF_Decals_RandomColor".Translate()))
            {
                var p = AllColors();
                if (p.Count > 0)
                {
                    color = p[Rand.Range(0, p.Count)];
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            if (!original.IndistinguishableFrom(color))
            {
                if (isArmor) _profileSet.Armor.SymbolColor = color;
                else _profileSet.Helmet.SymbolColor = color;
                PushLive();
            }
        }

        // Caches south-facing textures for grid thumbnails
        private Texture2D? GetThumb(DecalSymbolDef def)
        {
            if (def.Path.NullOrEmpty()) return null;
            _thumbCache ??= new Dictionary<string, Texture2D>();
            if (_thumbCache.TryGetValue(def.Path, out Texture2D cached)) return cached;

            Texture2D tex = ContentFinder<Texture2D>.Get(def.Path + "_south", false);
            if (tex == null) tex = ContentFinder<Texture2D>.Get(def.Path, false);
            _thumbCache[def.Path] = tex;
            return tex;
        }

        // Footer buttons
        private void DrawBottomButtons(Rect rect)
        {
            float w = 110f;
            float btnY = rect.y + 6f;
            float x = rect.xMax - (w * 3 + 20f);

            if (Widgets.ButtonText(new Rect(x, btnY, w, 32f), "BNF_Decals_Reset".Translate()))
            {
                _profileSet = _original;
                _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
                _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
                SyncSelection();
                PushLive();
            }
            if (Widgets.ButtonText(new Rect(x + w + 10f, btnY, w, 32f), "BNF_Decals_Apply".Translate()))
            {
                _committed = true;
                Close();
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - w, btnY, w, 32f), "Close".Translate()))
            {
                _committed = false;
                Close();
            }
        }

        private int FindSymbolIndex(string path)
        {
            if (path.NullOrEmpty() || _symbols.Count == 0) return 0;
            return Mathf.Max(0, _symbols.FindIndex(d => d.Path == path));
        }

        private void SyncSelection()
        {
            if (_symbols.Count == 0) return;
            _selectedHelmetIndex = Mathf.Clamp(_selectedHelmetIndex, 0, _symbols.Count - 1);
            _profileSet.Helmet.SymbolPath = _symbols[_selectedHelmetIndex].Path;
            _selectedArmorIndex = Mathf.Clamp(_selectedArmorIndex, 0, _symbols.Count - 1);
            _profileSet.Armor.SymbolPath = _symbols[_selectedArmorIndex].Path;
        }

        // Sorts by hue so it doesn't look like ass
        private List<Color> AllColors()
        {
            if (_allColors != null) return _allColors;
            HashSet<Color> colorSet = new HashSet<Color>();
            if (TryGetIdeoColor(_pawn, out Color ideo)) colorSet.Add(ideo);
            if (TryGetFavoriteColor(_pawn, out Color fav)) colorSet.Add(fav);
            foreach (var def in DefDatabase<ColorDef>.AllDefsListForReading)
                if (def.colorType == ColorType.Ideo || def.colorType == ColorType.Misc || def.colorType == ColorType.Structure)
                    colorSet.Add(def.color);
            _allColors = new List<Color>(colorSet);
            _allColors.Sort((a, b) =>
            {
                Color.RGBToHSV(a, out float hA, out float sA, out _);
                Color.RGBToHSV(b, out float hB, out float sB, out _);
                int c = hA.CompareTo(hB);
                return (c != 0) ? c : sA.CompareTo(sB);
            });
            return _allColors;
        }

        private void PushLive() => DecalUtil.SetLiveEditFull(_pawn, _profileSet);

        // These break if Ideology isn't active obviously
        private static bool TryGetIdeoColor(Pawn? pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive || pawn?.Ideo == null || Find.IdeoManager.classicMode) return false;
            c = pawn.Ideo.ApparelColor;
            return true;
        }

        private static bool TryGetFavoriteColor(Pawn? pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive || pawn?.story == null || pawn.DevelopmentalStage.Baby()) return false;
            ColorDef def = pawn.story.favoriteColor;
            if (def == null) return false;
            c = def.color;
            return true;
        }
    }
}