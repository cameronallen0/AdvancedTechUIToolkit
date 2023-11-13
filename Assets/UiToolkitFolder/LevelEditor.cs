using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

public class LevelEditor : EditorWindow
{
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private float spawnRadius;
    private float spacingR;
    private float minScale;
    private float maxScale;

    private Vector3 spawnLoc;
    private ObjectField prefabField;
    private ObjectField spawnField;
    private ObjectField parentField;
    private IntegerField spawnRadiusField;
    private IntegerField numberOfObjectsField;
    private FloatField spacingField;

    private FloatField minScaleField;
    private FloatField maxScaleField;

    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelEditor>();
        window.titleContent = new GUIContent("Level Editor");
    }

    private void OnEnable()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UiToolkitFolder/LevelEditorUXML.uxml");
        var root = rootVisualElement;
        visualTree.CloneTree(root);

        parentField = root.Q<ObjectField>("parentField");
        parentField.objectType = typeof(GameObject);

        spawnField = root.Q<ObjectField>("spawnLoc");
        spawnField.objectType = typeof(GameObject);

        spawnRadiusField = root.Q<IntegerField>("spawnRadius");
        spawnRadiusField.value = (int)spawnRadius;

        prefabField = root.Q<ObjectField>("objectField");
        prefabField.objectType = typeof(GameObject);

        minScaleField = root.Q<FloatField>("minScale");
        minScaleField.value = minScale;

        maxScaleField = root.Q<FloatField>("maxScale");
        maxScaleField.value = maxScale;

        var spawnButton = root.Q<Button>("spawn");
        spawnButton.clicked += SpawnButtonClicked;

        spacingField = root.Q<FloatField>("spacing");
        spacingField.value = spacingR;

        numberOfObjectsField = root.Q<IntegerField>("numberOfObjects");

        var numberButton = root.Q<Button>("spawnNumber");
        numberButton.clicked += SpawnNumberOfObjects;

        var updateButton = root.Q<Button>("updateSettings");
        updateButton.clicked += UpdateFoliageSettings;

        var deleteButton = root.Q<Button>("delete");
        deleteButton.clicked += DeleteButtonClicked;

        var deleteAllButton = root.Q<Button>("deleteAll");
        deleteAllButton.clicked += DeleteAllButtonClicked;
    }

    private void UpdateFoliageSettings()
    {
        GameObject spawnArea = spawnField.value as GameObject;

        spawnLoc = spawnArea.transform.position;
        Debug.Log(spawnLoc);

        spawnRadius = spawnRadiusField.value;
        Debug.Log("The spawn radius is " + spawnRadius);

        spacingR = spacingField.value;
        Debug.Log("The Prefab Spacing is " + spacingR);

        minScale = minScaleField.value;
        maxScale = maxScaleField.value;
    }

    private void SpawnButtonClicked()
    {
        GameObject pObject = SpawnObject();
        spawnedObjects.Add(pObject);
    }

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

    private GameObject SpawnObject()
    {
        float randomX, randomZ;
        float pSizeX, pSizeY, pSizeZ;
        Vector3 spawnPosition;

        GameObject prefab = prefabField.value as GameObject;
        GameObject parentObj = parentField.value as GameObject;

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

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                spawnPosition = new Vector3(randomX, hit.point.y, randomZ);

                foreach (var pObject in spawnedObjects)
                {
                    if (Vector3.Distance(spawnPosition, pObject.transform.position) < spacingR)
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    GameObject pObject = Instantiate(prefab, new Vector3(randomX, hit.point.y, randomZ), randomRotation);
                    pObject.transform.position = spawnPosition;
                    pObject.transform.parent = parentObj.transform;
                    pObject.transform.localScale = new Vector3(pSizeX, pSizeY, pSizeZ);
                    pObject.name = (prefab.name) + ("_") + (spawnedObjects.Count + 1);
                    return pObject;
                }
            }

            attempt++;
        } while (attempt < maxAttempts);

        Debug.LogWarning("No spawning spaces in radius");
        return null;
    }


    private void DeleteButtonClicked()
    {
        if (spawnedObjects.Count > 0)
        {
            GameObject objectToDelete = spawnedObjects[spawnedObjects.Count - 1];
            spawnedObjects.RemoveAt(spawnedObjects.Count - 1);
            DestroyImmediate(objectToDelete);
        }
    }

    private void DeleteAllButtonClicked()
    {
        foreach (var pObject in spawnedObjects)
        {
            DestroyImmediate(pObject);
        }
        spawnedObjects.Clear();
    }
}



