﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class GameStateManager : MonoBehaviour
{

    [SerializeField] List<GameObject> _currentWeapons = new List<GameObject>();
    [SerializeField] List<GameObject> _currentHeroes = new List<GameObject>();

    [SerializeField] List<GameObject> currentUnusedHeroes = new List<GameObject>();

    [SerializeField] GameObject heroPrefab;

    [SerializeField] private int currentgold;
    [SerializeField] private int totalGoldFromStage;

    [SerializeField] private int blacksmithMaxHealth = 100;
    [SerializeField] private int blacksmithCurrentHealth;

    [SerializeField] private int _currentStageIndex = 0;

    private static GameStateManager _gameStateManager;

    [SerializeField] private List<Stage> _gameStages;

    public static GameStateManager Instance { get => _gameStateManager;}
    public int CurrentStageIndex { get => _currentStageIndex;}
    public List<Stage> GameStages { get => _gameStages;}
    public List<GameObject> CurrentWeapons { get => _currentWeapons;}
    public List<GameObject> CurrentHeroes { get => _currentHeroes;}

    public float spacingBetweenHeroes = 0.75f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddWeapon(FindObjectOfType<WeaponCreationSystem>().CreateTestWeapon());
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            WinStage(FindObjectOfType<WaveManager>().currentStage);
        }

    }
    private void Awake()
    {

        //Singleton 
        if (_gameStateManager != null && _gameStateManager != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _gameStateManager = this;
        }


        DontDestroyOnLoad(gameObject);
        InitializeFirstTime();
    }
    private void InitializeFirstTime()
    {
        blacksmithCurrentHealth = blacksmithMaxHealth;
        currentgold = 100;

        GameObject hero1 = Instantiate(heroPrefab);
        GameObject hero2 = Instantiate(heroPrefab);
        GameObject hero3 = Instantiate(heroPrefab);

        AddHero(hero1);
        AddHero(hero2);
        AddHero(hero3); 
    }

    private void transitionToBattle()
    {
        StoreWeapons();
        StoreHeroes();

        MoveUnusedHeroes();

        StartCoroutine(LoadBattleScene());
    }

    private IEnumerator LoadBattleScene()
    {
        // Start loading the scene
        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(1);
        // Wait until the level finish loading
        while (!asyncLoadLevel.isDone)
            yield return null;
        // Wait a frame so every Awake and Start method is called

        HideHeroes();
        HideWeapons();

        AssignWeaponsToHeroes();

        InstantiateHeroUI();

        WaveManager waveManager = FindObjectOfType<WaveManager>();

        //If null reference add the test stage to game manager stage list stuff
        waveManager.currentStage = _gameStages[_currentStageIndex];
       
        waveManager.StartCoroutine(waveManager.StartSpawning());


        yield return new WaitForEndOfFrame();
    }

    private void AssignWeaponsToHeroes()
    {
        for (int i = 0; i < _currentHeroes.Count; i++)
        {
            HeroController heroController = _currentHeroes[i].GetComponent<HeroController>();
            _currentWeapons[i].GetComponent<Animator>().enabled = false;

            heroController.weaponObject = _currentWeapons[i];
            heroController.InitializeWeaponPosition();
            heroController.InitalizeWeaponData();
        }
    }

    private IEnumerator LoadShopScene()
    {
        // Start loading the scene
        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(0);
        // Wait until the level finish loading
        while (!asyncLoadLevel.isDone)
            yield return null;
        // Wait a frame so every Awake and Start method is called

        CalculateGold();

        blacksmithCurrentHealth = blacksmithMaxHealth;

        yield return new WaitForEndOfFrame();
    }

    private void MoveUnusedHeroes()
    {
        int numberOfunusedHeroes = _currentHeroes.Count - _currentWeapons.Count;

        for (int i = 0; i < numberOfunusedHeroes; i++)
        {
            int movedHeroIndex = _currentHeroes.Count - 1;

            currentUnusedHeroes.Add(_currentHeroes[movedHeroIndex]);
            _currentHeroes.RemoveAt(movedHeroIndex);

        }

    }

    public bool CanBuyWeapon(int price)
    {
        //returns true if there are enough heroes to buy it

        if (currentgold <= price)
        {
            return true;
        }
        else return false;
    }

    public bool HasEnougHeroes()
    {
        //returns true if there are enough heroes to buy it

        if (_currentWeapons.Count < _currentHeroes.Count)
        {
            return true;
        }
        else return false;
    }

    private void transitionToShop()
    {
        DeleteDeadHeroes();
        StoreHeroes();
        DeleteWeaponData();

        StartCoroutine(LoadShopScene());


    }

    public void WinStage(Stage wonStage)
    {
        for (int i = 0; i < wonStage.heroReward; i++)
        {
            GameObject heroObj = Instantiate(heroPrefab);

            AddHero(heroObj);
        }

        _currentStageIndex++;

        transitionToShop();
    }

    public void StartBattle()
    {
        transitionToBattle();
    }

    #region Data_Manipulation
    public void AddHero(GameObject heroObject)
    {
        _currentHeroes.Add(heroObject);
    }

    public void BuyWeapon(int price, GameObject weapon)
    {
        AddWeapon(weapon);
        currentgold -= price;
    }

    private void AddWeapon(GameObject weapon)
    {
        _currentWeapons.Add(weapon);
    }


    private void StoreWeapons()
    {
        //we make sure they don't destroy
        foreach (GameObject weaponObject in _currentWeapons)
        {
            DontDestroyOnLoad(weaponObject);
        }
    }

    private void StoreHeroes()
    {
        foreach (GameObject hero in _currentHeroes)
        {
            DontDestroyOnLoad(hero);
        }
    }

    private void DeleteDeadHeroes()
    {
        //Heroes get destroyed when they die otherwise we keep them
        for (int i = 0; i < _currentHeroes.Count; i++)
        {
            //check if they are destroyed and clean up the node
            if (_currentHeroes[i] == null)
            {
                _currentHeroes.RemoveAt(i);
            }
        }
    }

    private void DeleteWeaponData()
    {
        foreach (GameObject weaponObject in _currentWeapons)
        {
            Destroy(weaponObject);
        }
        _currentWeapons = new List<GameObject>();
    }
    #endregion

    #region Battle_Related
    private void HideHeroes()
    {
        for (int i = 0; i < _currentHeroes.Count; i++)
        {
            _currentHeroes[i].transform.position = new Vector3((i + 1), 0, -10);
        }

    }

    private void HideWeapons()
    {
        for (int i = 0; i < _currentWeapons.Count; i++)
        {
            _currentWeapons[i].transform.position = new Vector3(-(i + 1), 0, -10);
        }
    }
    private void InstantiateHeroUI()
    {
        Vector3 heroPosition = new Vector3(-1.5f, 0, 5);
        for (int i = 0; i < _currentHeroes.Count; i++)
        {
            _currentHeroes[i].transform.position = heroPosition;
            heroPosition = new Vector3(heroPosition.x + spacingBetweenHeroes, heroPosition.y, heroPosition.z);
        }
        for (int i = 0; i < currentUnusedHeroes.Count; i++)
        {
            currentUnusedHeroes[i].transform.position = new Vector3(100, 100, 100);
        }
    }

    public void DamageBlacksmith(int damage)
    {
        blacksmithCurrentHealth -= damage;

        if (blacksmithCurrentHealth <= 0)
        {
            LoseGame();
        }
    }

    private void LoseGame()
    {
        SceneManager.LoadScene(0);

        Debug.Log("You lost the game dingus");
    }

    #endregion

    #region Shop_Related
    private void CalculateGold()
    {
        int baseGold = 100 * _currentStageIndex;

        currentgold = baseGold + totalGoldFromStage;

        totalGoldFromStage = 0;
    }

    #endregion


}