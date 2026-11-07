using System;
using System.Collections.Generic;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 세계지도 창(가상 px): 화면 세로를 꽉 채운 지도를 가운데에 두고, 왼쪽 여백에 제목, 오른쪽 여백에 닫기 단추를 둔다.
    /// 그림과 구역 자료는 생성기가 만든다(Tools/PixelGen/world_map.py → Art/PixelGen/UI/WorldMap). 그림의 점 두 개가 가상 1px이다.
    /// 크게 보는 곳(힐리아)을 누르면 지도가 커지며 화면을 채우고, 그때의 점 자리에 맞춰 놓아 둔 세부 지역 이름이 켜진다.
    /// 메뉴보다 위, 인벤토리보다 아래에 그린다. 화면을 덮는 판이 누름을 받아 아래 화면이 눌리지 않는다.
    /// </summary>
    internal static class WorldMapUiBuilder
    {
        private const string Art = WorldAtlasImporter.Art;
        private const string WorldTitle = "세계지도";
        private const float DotsPerPixel = WorldAtlasImporter.DotsPerPixel;
        private const float NameHeight = 8f;
        private const float NameGap = 3.5f;

        private static readonly Vector2 MapSize = new Vector2(304f, 216f);
        private static readonly Vector2 PinSize = new Vector2(5.5f, 7.5f);
        private static readonly Color NamePlate = new Color(10f / 255f, 9f / 255f, 18f / 255f, 0.72f);
        private static readonly Color TownDot = new Color32(0xf6, 0xe2, 0x7a, 0xff);
        private static readonly Color FutureTownDot = new Color32(0x8a, 0x96, 0xa0, 0xff);

        public static WorldMapView Build(AtlasFile file)
        {
            Canvas canvas = NewCanvas("World Map UI", 33, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            ClickArea(screen).color = Night;
            RectTransform frame = VirtualFrame(screen);
            var worldPosition = new Vector2((VirtualScreen.x - MapSize.x) / 2f, 0f);
            RectTransform map = TopLeft(frame, "Map", worldPosition.x, worldPosition.y, MapSize.x, MapSize.y);
            Solid(map, Color.white).sprite = Sprite("world_map_blur");

            FocusRecord focus = file.focus;
            RectTransform names = TopLeft(frame, "Names", 0f, 0f, VirtualScreen.x, VirtualScreen.y);
            float pixelsPerDot = Mathf.Min(VirtualScreen.x / focus.width, VirtualScreen.y / focus.height);
            Vector2 focusPosition = FocusPosition(focus, pixelsPerDot);

            var pieces = new WorldMapView.AreaPiece[file.areas.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                AreaRecord area = file.areas[i];
                RectTransform piece = TopLeft(map, area.id, area.x / DotsPerPixel, area.y / DotsPerPixel, area.width / DotsPerPixel, area.height / DotsPerPixel);
                Solid(piece, Color.white).sprite = Sprite($"area_{area.id}");
                // 구역 조각의 자식이라 그 구역을 다녀왔을 때만 보인다.
                AddRoutes(piece, new Vector2(area.x, area.y) / DotsPerPixel, RoadEndsIn(file, area.id), PlacesIn(file, area.id), TownDot);
                RectTransform group = TopLeft(names, area.id, 0f, 0f, VirtualScreen.x, VirtualScreen.y);
                foreach (NameRecord name in file.names)
                {
                    if (name.area == area.id)
                    {
                        AddName(group, name, focusPosition, pixelsPerDot);
                    }
                }

                pieces[i] = new WorldMapView.AreaPiece(area.id, piece.gameObject, group.gameObject);
            }

            AddRoutes(map, Vector2.zero, Array.Empty<Vector2>(), Array.ConvertAll(file.futureTowns, town => new Vector2(town.x, town.y) / DotsPerPixel), FutureTownDot);

            RectTransform focusRect = TopLeft(map, "Focus", focus.x / DotsPerPixel, focus.y / DotsPerPixel, focus.width / DotsPerPixel, focus.height / DotsPerPixel);
            var focusButton = focusRect.gameObject.AddComponent<Button>();
            focusButton.targetGraphic = ClickArea(focusRect);
            focusButton.transition = Selectable.Transition.None;

            RectTransform here = Rect(map, "Here", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0f), Vector2.zero, PinSize);
            Solid(here, Color.white).sprite = Sprite("here");
            names.SetAsLastSibling();

            TMP_Text title = Label(frame, "Title", 4f, 6f, 60f, 12f, 8.5f, Bold, Gold, TextAlignmentOptions.TopLeft);
            title.text = WorldTitle;
            Button back = WindowButton(TopLeft(frame, "Back", 4f, 20f, 32f, 14f), "전체", 7.5f);
            Button close = CloseButton(frame, 360f, 3f, Gold);

            var view = canvas.gameObject.AddComponent<WorldMapView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("closeButton", close), ("pieces", pieces), ("here", here),
                ("map", map), ("focusButton", focusButton), ("backButton", back), ("names", names.gameObject), ("title", title),
                ("worldTitle", WorldTitle), ("focusTitle", focus.title), ("worldPosition", new Vector2(worldPosition.x, -worldPosition.y)),
                ("focusPosition", focusPosition), ("focusScale", pixelsPerDot * DotsPerPixel));
            return view;
        }

        /// <summary>지도 전체를 덮는 길과 점. origin은 부모가 지도에서 차지한 왼쪽 위 자리다.</summary>
        private static void AddRoutes(RectTransform parent, Vector2 origin, Vector2[] roadEnds, Vector2[] dots, Color dotColor)
        {
            RectTransform rect = TopLeft(parent, "Routes", -origin.x, -origin.y, MapSize.x, MapSize.y);
            var routes = rect.gameObject.AddComponent<WorldMapRoutes>();
            routes.raycastTarget = false;
            SerializedFields.Set(routes, ("roadEnds", roadEnds), ("dots", dots), ("dotColor", dotColor));
        }

        private static Vector2[] RoadEndsIn(AtlasFile file, string area)
        {
            var ends = new List<Vector2>();
            foreach (RoadRecord road in file.roads)
            {
                if (road.area == area)
                {
                    ends.Add(new Vector2(road.ax, road.ay) / DotsPerPixel);
                    ends.Add(new Vector2(road.bx, road.by) / DotsPerPixel);
                }
            }

            return ends.ToArray();
        }

        private static Vector2[] PlacesIn(AtlasFile file, string area)
        {
            var dots = new List<Vector2>();
            foreach (PlaceRecord place in file.places)
            {
                if (place.area == area)
                {
                    dots.Add(new Vector2(place.x, place.y) / DotsPerPixel);
                }
            }

            return dots.ToArray();
        }

        /// <summary>크게 볼 곳이 화면 가운데에 오는 지도 왼쪽 위 자리. 지도 아래 끝이 화면 안으로 올라와 빈틈이 생기지 않게 한다.</summary>
        private static Vector2 FocusPosition(FocusRecord focus, float pixelsPerDot)
        {
            float mapHeight = MapSize.y * DotsPerPixel * pixelsPerDot;
            float left = (VirtualScreen.x - focus.width * pixelsPerDot) / 2f - focus.x * pixelsPerDot;
            float top = (VirtualScreen.y - focus.height * pixelsPerDot) / 2f - focus.y * pixelsPerDot;
            return new Vector2(left, -Mathf.Max(top, VirtualScreen.y - mapHeight));
        }

        /// <summary>어두운 판에 얹은 이름 하나. 크게 본 화면에서 그 점의 바로 위(below면 아래)에 놓는다.</summary>
        private static void AddName(Transform parent, NameRecord name, Vector2 focusPosition, float pixelsPerDot)
        {
            var dot = new Vector2(focusPosition.x + name.x * pixelsPerDot, -focusPosition.y + name.y * pixelsPerDot);
            float size = name.region ? 8f : 6f;
            float centerY = name.region ? dot.y : name.below ? dot.y + NameGap + NameHeight / 2f : dot.y - NameGap - NameHeight / 2f;
            TMP_Text text = Text(parent, name.text, name.text, Bold, size, name.region ? Gold : Ink, TextAlignmentOptions.Center);
            float width = text.GetPreferredValues(name.text, 200f, 20f).x + 4f;
            float height = name.region ? 11f : NameHeight;
            RectTransform plate = TopLeft(parent, name.text + " Plate", dot.x - width / 2f, centerY - height / 2f, width, height);
            Solid(plate, NamePlate);
            text.transform.SetParent(plate, false);
            FillParent(text.rectTransform);
        }

        private static Sprite Sprite(string name)
        {
            return WebGameImporter.ImportSprite($"{Art}/{name}.png", new Vector2(0.5f, 0.5f), 2048, FilterMode.Point);
        }
    }
}
