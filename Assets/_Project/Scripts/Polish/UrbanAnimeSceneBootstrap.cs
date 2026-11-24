using System.Collections.Generic;
using UnityEngine;
using NeoVeil.City;
using NeoVeil.Quest;

namespace NeoVeil.Polish
{
    /// <summary>
    /// 1-Click District 07 Greybox Layout Generator for Unity 6 URP!
    /// Attach to an empty GameObject, right-click the component header in Inspector,
    /// and choose "Generate District 07 Greybox Layout" to instantly spawn:
    /// - Neon Plaza asphalt floor + surrounding anime skyscraper silhouettes & glowing signs
    /// - Streetlamp Point Lights (automatically wired to DayNightNeonController if present)
    /// - Cafe 07 Hub Terminal + Barista Ren NPC
    /// - 2 Metro Fast Travel Stops (Neon Plaza & Cafe 07)
    /// - 1 Anomaly Rift Zone + linked Anomaly Crate
    /// </summary>
    public class UrbanAnimeSceneBootstrap : MonoBehaviour
    {
        [Header("District Layout Settings")]
        [SerializeField, Min(20f)] private float plazaRadius = 28f;
        [SerializeField, Min(6)] private int buildingCount = 18;
        [SerializeField] private bool generateStreetLights = true;

        private static readonly Color[] NeonPalette =
        {
            new Color(0.13f, 0.83f, 0.93f), // Cyan
            new Color(0.95f, 0.35f, 0.72f), // Magenta
            new Color(0.98f, 0.68f, 0.15f), // Amber
            new Color(0.66f, 0.42f, 0.98f)  // Violet
        };

        [ContextMenu("Generate District 07 Greybox Layout")]
        public void GenerateDistrictLayout()
        {
            Transform existing = transform.Find("District07_Generated");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("District07_Generated").transform;
            root.SetParent(transform, false);

            // 1. Asphalt Plaza Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Plaza_AsphaltFloor";
            floor.transform.SetParent(root, false);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(plazaRadius * 2.4f, 1f, plazaRadius * 2.4f);
            ApplyColor(floor, new Color(0.1f, 0.11f, 0.16f));

            // 2. Perimeter Urban Skyscrapers + Neon Signboards
            var buildingsParent = new GameObject("Skyscrapers").transform;
            buildingsParent.SetParent(root, false);
            var lightsList = new List<Light>();

            for (int i = 0; i < buildingCount; i++)
            {
                float angle = (i / (float)buildingCount) * Mathf.PI * 2f;
                float dist = plazaRadius + 5f + (i % 3) * 4f;
                float height = 10f + ((i * 7) % 18);
                float width = 5f + (i % 3) * 1.5f;

                Vector3 bPos = new Vector3(Mathf.Cos(angle) * dist, height * 0.5f, Mathf.Sin(angle) * dist);
                GameObject bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bld.name = $"Building_{i + 1:00}";
                bld.transform.SetParent(buildingsParent, false);
                bld.transform.position = bPos;
                bld.transform.localScale = new Vector3(width, height, width);
                ApplyColor(bld, new Color(0.13f, 0.15f, 0.24f));

                // Emissive Neon Signboard facing plaza
                Color neonCol = NeonPalette[i % NeonPalette.Length];
                GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = $"NeonSign_{i + 1:00}";
                sign.transform.SetParent(bld.transform, false);
                Vector3 inward = new Vector3(-Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
                sign.transform.position = new Vector3(bPos.x, Mathf.Min(height * 0.65f, 8f), bPos.z) + inward * (width * 0.54f);
                sign.transform.rotation = Quaternion.LookRotation(inward, Vector3.up);
                sign.transform.localScale = new Vector3(2.8f, 1.1f, 0.2f);
                ApplyColor(sign, neonCol, emissiveBoost: 2.6f);

                if (generateStreetLights && i % 3 == 0)
                {
                    var lightObj = new GameObject($"StreetLamp_{i + 1:00}");
                    lightObj.transform.SetParent(buildingsParent, false);
                    lightObj.transform.position = new Vector3(Mathf.Cos(angle) * (plazaRadius - 2f), 5.5f, Mathf.Sin(angle) * (plazaRadius - 2f));
                    Light ptLight = lightObj.AddComponent<Light>();
                    ptLight.type = LightType.Point;
                    ptLight.color = neonCol;
                    ptLight.range = 16f;
                    ptLight.intensity = 3.2f;
                    lightsList.Add(ptLight);
                }
            }

            // 3. Cafe 07 Hub Area (Terminal + Barista Ren NPC)
            var cafeParent = new GameObject("Cafe07_Hub").transform;
            cafeParent.SetParent(root, false);

            GameObject cafeTerminalObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cafeTerminalObj.name = "Cafe07_Terminal";
            cafeTerminalObj.transform.SetParent(cafeParent, false);
            cafeTerminalObj.transform.position = new Vector3(-12f, 0.9f, -10f);
            cafeTerminalObj.transform.localScale = new Vector3(1.6f, 1.8f, 1.0f);
            ApplyColor(cafeTerminalObj, new Color(0.95f, 0.68f, 0.18f), emissiveBoost: 1.4f);
            cafeTerminalObj.AddComponent<CafeHubTerminal>();

            GameObject npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcObj.name = "NPC_BaristaRen";
            npcObj.transform.SetParent(cafeParent, false);
            npcObj.transform.position = new Vector3(-9.5f, 1.0f, -10f);
            ApplyColor(npcObj, new Color(0.2f, 0.85f, 0.95f));
            npcObj.AddComponent<CityNpcInteractable>();

            // 4. Metro Fast Travel Stops
            CreateMetroStop(root, "MetroStop_NeonPlaza", new Vector3(13f, 1.2f, 10f));
            CreateMetroStop(root, "MetroStop_CafeHub", new Vector3(-14f, 1.2f, -6f));

            // 5. Center Anomaly Rift + Nearby Anomaly Crate
            GameObject riftObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            riftObj.name = "AnomalyRift_NeonPlaza";
            riftObj.transform.SetParent(root, false);
            riftObj.transform.position = new Vector3(0f, 0.08f, 8f);
            riftObj.transform.localScale = new Vector3(3.6f, 0.08f, 3.6f);
            ApplyColor(riftObj, new Color(0.68f, 0.22f, 0.98f), emissiveBoost: 2.0f);
            riftObj.AddComponent<AnomalyRiftZone>();

            GameObject crateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crateObj.name = "Crate_NeonPlaza";
            crateObj.transform.SetParent(root, false);
            crateObj.transform.position = new Vector3(4.5f, 0.55f, 11.5f);
            crateObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            ApplyColor(crateObj, new Color(0.15f, 0.9f, 0.95f), emissiveBoost: 1.2f);
            crateObj.AddComponent<AnomalyCrate>();

            Debug.Log($"[NeoVeil Bootstrap] District 07 Greybox Layout berhasil dibuat! ({buildingCount} gedung, {lightsList.Count} lampu jalan, Cafe 07, 2 Halte Metro, Anomaly Rift & Crate).");
        }

        private static void CreateMetroStop(Transform parent, string objName, Vector3 pos)
        {
            GameObject stop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stop.name = objName;
            stop.transform.SetParent(parent, false);
            stop.transform.position = pos;
            stop.transform.localScale = new Vector3(1.0f, 2.4f, 0.5f);
            ApplyColor(stop, new Color(0.15f, 0.85f, 1f), emissiveBoost: 1.5f);
            stop.AddComponent<MetroFastTravelStop>();
        }

        private static void ApplyColor(GameObject obj, Color color, float emissiveBoost = 0f)
        {
            var rend = obj.GetComponent<Renderer>();
            if (rend == null) return;
            var mat = new Material(rend.sharedMaterial);
            mat.color = color;
            if (emissiveBoost > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emissiveBoost);
            }
            rend.sharedMaterial = mat;
        }
    }
}
