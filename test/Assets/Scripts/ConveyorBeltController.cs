using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorBeltController : MonoBehaviour
{
    public Transform spawnPoint; // Точка спавна предметов
    public GameObject[] trashPrefabs; // Префабы мусора
    public GameObject[] usefulItemsPrefabs; // Префабы полезных предметов
    public float spawnInterval = 1f; // Интервал между спавном предметов
    public float conveyorSpeed = 2f; // Скорость движения конвейера
    private float timeSinceLastSpawn;

    private void Start()
    {
        StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
    {
        yield return new WaitForSeconds(3); // Задержка перед началом игры
        ScoreManager.Instance.StartTimer();
        StartCoroutine(SpawnObjects());
    }

    private IEnumerator SpawnObjects()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnRandomObject();
        }
    }

    private void SpawnRandomObject()
    {
        int randomIndex = Random.Range(0, trashPrefabs.Length + usefulItemsPrefabs.Length);

        if (randomIndex < trashPrefabs.Length)
        {
            Instantiate(trashPrefabs[randomIndex], spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Instantiate(usefulItemsPrefabs[randomIndex - trashPrefabs.Length], spawnPoint.position, Quaternion.identity);
        }
    }

    private void Update()
    {
        MoveObjectsOnConveyor();
    }

    private void MoveObjectsOnConveyor()
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("ConveyorItem");

        foreach (GameObject obj in objects)
        {
            Vector3 newPosition = obj.transform.position;
            newPosition.z -= conveyorSpeed * Time.deltaTime;
            obj.transform.position = newPosition;

            if (newPosition.z < -10f) // Если предмет ушел за пределы экрана
            {
                Destroy(obj);
            }
        }
    }
}