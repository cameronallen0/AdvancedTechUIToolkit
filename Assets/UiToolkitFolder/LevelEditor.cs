using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

public class LevelEditor : EditorWindow
{
    //list for spawned objects
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    //list for prefabs in folder
    public GameObject[] folderObjects;
    public static List<GameObject> folderList = new List<GameObject>();

    //spawn and parent variable
    private Vector3 spawnLoc;
    private ObjectField spawnField;
    private ObjectField parentField;
    private float spawnRadius;
    private float spacingR;

    private IntegerField spawnRadiusField;
    private IntegerField numberOfObjectsField;
    private FloatField spacingField;

    //scale variables
    private FloatField minScaleField;
    private FloatField maxScaleField;
    private float minScale;
    private float maxScale;

    //layer variables
    private LayerMaskField layerMaskField;
    private LayerMask groundLayerMask;

    //prefab creator
    private TextField prefabNameField;
    private TextField prefabShapeField;

    [MenuItem("Tools/Foliage Tool")]
    public static void ShowWindow()
    {
        LevelEditor window = GetWindow<LevelEditor>();
        window.titleContent = new GUIContent("Foliage Tool");
    }

    private void OnEnable()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UiToolkitFolder/FoliageToolUXML.uxml");
        var root = rootVisualElement;
        visualTree.CloneTree(root);

        spawnField = root.Q<ObjectField>("spawnArea");
        spawnField.objectType = typeof(GameObject);

        parentField = root.Q<ObjectField>("parentObject");
        parentField.objectType = typeof(GameObject);

        spacingField = root.Q<FloatField>("prefabSpacing");
        spacingField.value = spacingR;

        minScaleField = root.Q<FloatField>("minScale");
        minScaleField.value = minScale;

        maxScaleField = root.Q<FloatField>("maxScale");
        maxScaleField.value = maxScale;

        spawnRadiusField = root.Q<IntegerField>("spawnRadius");
        spawnRadiusField.value = (int)spawnRadius;

        numberOfObjectsField = root.Q<IntegerField>("numberObjects");

        layerMaskField = root.Q<LayerMaskField>("layerMask");
        layerMaskField.value = groundLayerMask;

        var updateButton = root.Q<Button>("updateSettings");
        updateButton.clicked += UpdateFoliageSettings;

        var spawnButton = root.Q<Button>("spawnPrefab");
        spawnButton.clicked += SpawnNumberOfObjects;

        var deleteButton = root.Q<Button>("deleteLatest");
        deleteButton.clicked += DeleteButtonClicked;

        var deleteAllButton = root.Q<Button>("deleteAll");
        deleteAllButton.clicked += DeleteAllButtonClicked;

        folderObjects = Resources.LoadAll<GameObject>("Prefabs");
        foreach (GameObject i in folderObjects)
        {
            folderList.Add(i);
        }

        prefabNameField = root.Q<TextField>("prefabName");

        prefabShapeField = root.Q<TextField>("prefabShape");

        var createPrefabButton = root.Q<Button>("createPrefab");
        createPrefabButton.clicked += () => CreatePrefab(prefabNameField.value, prefabShapeField.value);
    }

    //update all setting changes
    private void UpdateFoliageSettings()
    {
        GameObject spawnArea = spawnField.value as GameObject;

        spawnLoc = spawnArea.transform.position;

        spawnRadius = spawnRadiusField.value;

        spacingR = spacingField.value;

        minScale = minScaleField.value;
        maxScale = maxScaleField.value;

        groundLayerMask = layerMaskField.value;
    }

    //sets number of prefabs to spawn
    private void SpawnNumberOfObjects()
    {
        int numToSpawn = numberOfObjectsField.value;
        for (int i = 0; i < numToSpawn; i++)
        {
            GameObject pObject = SpawnObject();
            if (pObject != null)
            {
                spawnedObjects.Add(pObject);
            }
        }
    }

    //handles spawning of prefabs, raycast for location and has rotation
    //sets parent of object
    private GameObject SpawnObject()
    {
        float randomX, randomZ;
        float pSizeX, pSizeY, pSizeZ;
        Vector3 spawnPosition;

        GameObject parentObj = parentField.value as GameObject;

        int v = Random.Range(0, folderList.Count);
        int folderIndex = v;

        int maxAttempts = 10;
        int attempt = 0;
        do
        {
            randomX = Random.Range(spawnLoc.x - spawnRadius, spawnLoc.x + spawnRadius);
            randomZ = Random.Range(spawnLoc.z - spawnRadius, spawnLoc.z + spawnRadius);
            pSizeX = Random.Range(minScale, maxScale);
            pSizeY = pSizeX;
            pSizeZ = pSizeX;

            Ray ray = new Ray(new Vector3(randomX, 1000f, randomZ), Vector3.down);

            RaycastHit hit;

            Quaternion randomRotation = Quaternion.Euler(
                Random.Range(0f, 0f),
                Random.Range(0f, 360f),
                Random.Range(0f, 0f));

            bool overlap = false;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerMask))
            {
                spawnPosition = new Vector3(randomX, hit.point.y, randomZ);

                foreach (var pObject in spawnedObjects)
                {
                    Vector3 otherSize = pObject.transform.localScale;
                    Vector3 otherHalfSize = otherSize / 2f;

                    Vector3 thisHalfSize = new Vector3(pSizeX, pSizeY, pSizeZ) / 2f;

                    if (Vector3.Distance(spawnPosition, pObject.transform.position) <
                        (spacingR + Vector3.Distance(thisHalfSize, otherHalfSize)))
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    GameObject pObject = Instantiate(folderList[folderIndex], spawnPosition, randomRotation);
                    pObject.transform.parent = parentObj.transform;
                    pObject.transform.localScale = new Vector3(pSizeX, pSizeY, pSizeZ);
                    pObject.name = folderList[folderIndex].name + "_" + (spawnedObjects.Count + 1);
                    spawnedObjects.Add(pObject);
                    return pObject;
                }
            }

            attempt++;
        } while (attempt < maxAttempts);

        Debug.LogWarning("No spawning spaces in radius");
        return null;
    }


    //deletes the latest spawned object
    private void DeleteButtonClicked()
    {
        if (spawnedObjects.Count > 0)
        {
            GameObject objectToDelete = spawnedObjects[spawnedObjects.Count - 1];
            spawnedObjects.RemoveAt(spawnedObjects.Count - 1);
            DestroyImmediate(objectToDelete);
        }
    }

    //delete all objects in list unless its been saved
    private void DeleteAllButtonClicked()
    {
        foreach (var pObject in spawnedObjects)
        {
            DestroyImmediate(pObject);
        }
        spawnedObjects.Clear();
    }

    //creates a basic cube prefab that can be named and is added to the prefabs folder
    private void CreatePrefab(string name, string shape)
    {
        var newPrefab = new GameObject();
        if(shape == "Cube")
        {
            GameObject childPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childPrefab.transform.parent = newPrefab.transform;
            childPrefab.transform.position = new Vector3(0, 0.5f, 0);
        }
        if(shape == "Sphere")
        {
            GameObject childPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            childPrefab.transform.parent = newPrefab.transform;
            childPrefab.transform.position = new Vector3(0, 0.5f, 0);
        }
        if (shape == "Cylinder")
        {
            GameObject childPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            childPrefab.transform.parent = newPrefab.transform;
            childPrefab.transform.position = new Vector3(0, 0.5f, 0);
        }

        folderList.Add(newPrefab);

        string prefabPath = "Assets/Resources/Prefabs/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(newPrefab, prefabPath);
        AssetDatabase.Refresh();

        Debug.Log(name + " saved to Prefabs folder");
    }
}