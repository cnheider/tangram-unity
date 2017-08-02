using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Mapzen.VectorData;
using Mapzen.VectorData.Filters;
using Mapzen;

public class MapzenMap : MonoBehaviour
{
    public string ApiKey = "vector-tiles-tyHL4AY";

    public TileArea Area = new TileArea(
                               new LngLat(-74.014892578125, 40.70562793820589),
                               new LngLat(-74.00390625, 40.713955826286046),
                               16);

    private List<GameObject> tiles = new List<GameObject>();

    private UnityIO tileIO = new UnityIO();

    [SerializeField]
    private string exportPath = "Assets/Generated";

    [SerializeField]
    private List<FeatureStyle> featureStyling = new List<FeatureStyle>();

    private List<TileTask> tasks = new List<TileTask>();

    private int nTasksForArea = 0;

    private SceneGroup area = new SceneGroup(SceneGroup.Type.None, "MapRegion");

    public void DownloadTiles()
    {
        TileBounds bounds = new TileBounds(Area);

        tasks.Clear();
        nTasksForArea = 0;

        foreach (var tileAddress in bounds.TileAddressRange)
        {
            nTasksForArea++;
        }

        foreach (var tileAddress in bounds.TileAddressRange)
        {
            var wrappedTileAddress = tileAddress.Wrapped();
            var uri = new Uri(string.Format("https://tile.mapzen.com/mapzen/vector/v1/all/{0}/{1}/{2}.mvt?api_key={3}",
                              wrappedTileAddress.z, wrappedTileAddress.x, wrappedTileAddress.y, ApiKey));

            Debug.Log("URL request " + uri.AbsoluteUri);

            UnityIO.IORequestCallback onTileFetched = (response) =>
            {
                if (response.hasError())
                {
                    Debug.Log("TileIO Error: " + response.error);
                    return;
                }

                if (response.data.Length == 0)
                {
                    Debug.Log("Empty Response");
                    return;
                }

                float offsetX = (tileAddress.x - bounds.min.x);
                float offsetY = (-tileAddress.y + bounds.min.y);

                SceneGroup.Type groupOptions =
                    SceneGroup.Type.Feature | SceneGroup.Type.Tile;
                // groupOptions = SceneGroup.Type.Tile;

                TileTask task = new TileTask(tileAddress, groupOptions, response.data, offsetX, offsetY);

                task.Start(featureStyling, area);

                OnTaskReady(task, groupOptions);
            };

            // Starts the HTTP request
            StartCoroutine(tileIO.FetchNetworkData(uri, onTileFetched));
        }
    }

    void OnTaskReady(TileTask readyTask, SceneGroup.Type groupOptions)
    {
        tasks.Add(readyTask);

        if (tasks.Count == nTasksForArea)
        {
            tasks.Clear();

            Visit(area, groupOptions, null);
        }
    }

    private void Visit(SceneGroup group, SceneGroup.Type groupOptions, Transform parent)
    {
        if (group.childs.Count == 0 && !SceneGroup.IsLeaf(group.type, groupOptions))
        {
            return;
        }

        var gameObject = new GameObject(group.name);

        if (parent != null)
        {
            gameObject.transform.parent = parent;
        }

        foreach (var child in group.childs)
        {
            Visit(child, groupOptions, gameObject.transform);
        }

        if (group.meshData != null && group.meshData.Vertices.Count > 0)
        {
            FeatureBehavior featureBehavior = gameObject.AddComponent<FeatureBehavior>();
            featureBehavior.CreateUnityMesh(group.meshData, 0, 0);
        }
    }

    public List<GameObject> Tiles
    {
        get
        {
            return tiles;
        }
    }


    public List<FeatureStyle> FeatureStyling
    {
        get { return featureStyling; }
    }

    public string ExportPath
    {
        get
        {
            return exportPath;
        }
        set
        {
            exportPath = value;
        }
    }
}
