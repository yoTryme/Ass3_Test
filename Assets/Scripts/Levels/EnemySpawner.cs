// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections;

// public class EnemySpawner : MonoBehaviour
// {
//     /* ---------- Inspector‑assigned fields ---------- */
//     [Header("UI")]
//     public Image      level_selector;      // panel holding the difficulty buttons
//     public GameObject buttonPrefab;        // prefab for Easy/Medium/Endless buttons
//     public TMP_Text   waveButtonLabel;     // OPTIONAL: drag in the Wave button's TMP_Text here
//     public GameObject rewardScreenPanel;   // the panel shown between waves

//     [Header("Spawn")]
//     public GameObject enemyPrefab;         // Enemy prefab to instantiate
//     public SpawnPoint[] SpawnPoints;       // Assign all spawn points here

//     /* ---------- internal state ---------- */
//     private int  waveIndex     = 0;        // current wave number (1-based)
//     private int  maxWaves      = 0;        // number of waves this level
//     private bool levelFinished = false;    // true once last wave of current level completes

//     /* ====================================================================== */
//     /*                         MAIN MENU SETUP                                */
//     /* ====================================================================== */
//     void Start()
//     {
//         // Build three difficulty buttons at runtime
//         string[] levels = { "Easy", "Medium", "Endless" };
//         float yStart = 130f, yStep = -50f;
//         for (int i = 0; i < levels.Length; i++)
//         {
//             GameObject btn = Instantiate(buttonPrefab, level_selector.transform);
//             btn.transform.localPosition = new Vector3(0, yStart + i * yStep, 0);

//             var m = btn.GetComponent<MenuSelectorController>();
//             m.spawner = this;
//             m.SetLevel(levels[i]);  // sets label text + stores the level name
//         }

//         // Load enemy stats once (from Resources/enemies.json)
//         TextAsset enemyJson = Resources.Load<TextAsset>("enemies");
//         GameManager.Instance.LoadEnemyData(enemyJson);
//     }

//     /* ====================================================================== */
//     /*                    START A NEW LEVEL (Menu button)                    */
//     /* ====================================================================== */
//     public void StartLevel(string levelName)
//     {
//         // Hide both the main menu and any reward screen
//         level_selector.gameObject.SetActive(false);
//         if (rewardScreenPanel != null)
//             rewardScreenPanel.SetActive(false);

//         GameManager.SelectedLevelName = levelName;

//         // Decide how many waves each difficulty runs
//         maxWaves = (levelName == "Easy")   ? 1 :
//                    (levelName == "Medium") ? 15 :
//                    int.MaxValue;               // Endless

//         waveIndex     = 0;
//         levelFinished = false;

//         // Kick off the player's StartLevel logic and first wave
//         GameManager.Instance.player
//             .GetComponent<PlayerController>().StartLevel();
//         StartCoroutine(SpawnWave());
//     }

//     /* ====================================================================== */
//     /*    NEXT‑WAVE BUTTON (RewardScreen) — either continue or next level     */
//     /* ====================================================================== */
//     public void NextWave()
//     {
//         if (levelFinished)
//         {
//             // Easy → Medium, Medium → Endless, Endless stays Endless
//             string next =
//                 (GameManager.SelectedLevelName == "Easy")   ? "Medium" :
//                 (GameManager.SelectedLevelName == "Medium") ? "Endless" :
//                                                                "Endless";

//             // Hide the reward panel before jumping to next level
//             if (rewardScreenPanel != null)
//                 rewardScreenPanel.SetActive(false);

//             StartLevel(next);
//         }
//         else
//         {
//             // Still in the same difficulty: spawn the next wave
//             StartCoroutine(SpawnWave());
//         }
//     }

//     // Overload retained for legacy calls (not used by the UI)
//     public void NextWave(string levelName) => StartCoroutine(SpawnWave());

//     /* ====================================================================== */
//     /*                        WAVE SPAWNING COROUTINE                         */
//     /* ====================================================================== */
//     IEnumerator SpawnWave()
//     {
//         waveIndex++;
//         Debug.Log($"Wave {waveIndex}/{maxWaves} — {GameManager.SelectedLevelName}");

//         // 3-second countdown
//         GameManager.Instance.state     = GameManager.GameState.COUNTDOWN;
//         GameManager.Instance.countdown = 3;
//         for (int i = 0; i < 3; i++)
//         {
//             yield return new WaitForSeconds(1);
//             GameManager.Instance.countdown--;
//         }

//         GameManager.Instance.state = GameManager.GameState.INWAVE;

//         // Choose enemy type by selected difficulty
//         string eType =
//             (GameManager.SelectedLevelName == "Medium")  ? "skeleton" :
//             (GameManager.SelectedLevelName == "Endless") ? "warlock"  :
//                                                            "zombie";

//         // Spawn 10 of that type
//         for (int i = 0; i < 10; i++)
//             yield return SpawnEnemy(eType);

//         // Wait until all spawned enemies are dead
//         yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);

//         // Mark whether this was the last wave
//         levelFinished = (waveIndex >= maxWaves);

//         // If level finished, update the button label
//         if (levelFinished)
//         {
//             if (waveButtonLabel != null)
//             {
//                 waveButtonLabel.text = "Next Level";
//             }
//             else
//             {
//                 // Fallback: find the WaveButton by name
//                 GameObject wb = GameObject.Find("WaveButton");
//                 if (wb != null)
//                 {
//                     var tmp = wb.GetComponentInChildren<TMP_Text>();
//                     if (tmp != null)
//                         tmp.text = "Next Level";
//                 }
//             }
//         }

//         // Signal end of wave so RewardScreen shows up
//         GameManager.Instance.state = GameManager.GameState.WAVEEND;
//     }

//     /* ====================================================================== */
//     /*                         SPAWN ONE ENEMY                                */
//     /* ====================================================================== */
//     IEnumerator SpawnEnemy(string type)
//     {
//         var info = GameManager.Instance.GetEnemyInfo(type);
//         if (info == null) yield break;

//         // Select a random spawn point + small offset
//         var sp  = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
//         var pos = sp.transform.position + (Vector3)(Random.insideUnitCircle * 1.8f);

//         // Instantiate and configure
//         var e = Instantiate(enemyPrefab, pos, Quaternion.identity);
//         e.GetComponent<SpriteRenderer>().sprite =
//             GameManager.Instance.enemySpriteManager.Get(info.sprite);

//         var ec = e.GetComponent<EnemyController>();
//         ec.maxHP     = ec.currentHP = info.hp;
//         ec.speed     = info.speed;
//         ec.damage    = info.damage;

//         GameManager.Instance.AddEnemy(e);
//         yield return new WaitForSeconds(0.5f);
//     }
// }


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("UI")]
    public Image level_selector;
    public GameObject buttonPrefab;
    public TMP_Text waveButtonLabel;
    public GameObject rewardScreenPanel;
    public TMP_Text HUDInfo;

    [Header("Spawn")]
    public GameObject enemyPrefab;
    public SpawnPoint[] SpawnPoints;

    int waveIndex = 0;
    int maxWaves = 0;
    bool levelFinished = false;
    int enemiesSpawned = 0;
    int killCount = 0;

    void Start()
    {
#if UNITY_2022_2_OR_NEWER
        if (SpawnPoints == null || SpawnPoints.Length == 0)
            SpawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
#else
        if (SpawnPoints == null || SpawnPoints.Length == 0)
            SpawnPoints = FindObjectsOfType<SpawnPoint>();
#endif

        string[] lvls = { "Easy", "Medium", "Endless" };
        float y0 = 130f, dy = -50f;
        for (int i = 0; i < lvls.Length; i++)
        {
            var b = Instantiate(buttonPrefab, level_selector.transform);
            b.transform.localPosition = new Vector3(0, y0 + i * dy, 0);
            var mc = b.GetComponent<MenuSelectorController>();
            mc.spawner = this;
            mc.SetLevel(lvls[i]);
        }

        if (HUDInfo) HUDInfo.gameObject.SetActive(false);

        var enemyJson = Resources.Load<TextAsset>("enemies");
        GameManager.Instance.LoadEnemyData(enemyJson);

        var levelJson = Resources.Load<TextAsset>("levels");
        GameManager.Instance.LoadLevelData(levelJson);
        Debug.Log("Loaded level: " + GameManager.Instance.LevelMap["Easy"].spawns.Count + " spawn groups.");

    }

    public void StartLevel(string levelName)
    {
        level_selector.gameObject.SetActive(false);
        if (rewardScreenPanel)
        { 
            rewardScreenPanel.SetActive(false);
            EventCenter.Broadcast(EventDefine.CloseModifierPart);
            EventCenter.Broadcast(EventDefine.CloseSpellPart);
        }

       

        GameManager.SelectedLevelName = levelName;
        var levelData = GameManager.Instance.LevelMap[levelName];
        maxWaves = levelData.waves > 0 ? levelData.waves : int.MaxValue;

        waveIndex = 0;
        levelFinished = false;

        if (HUDInfo) HUDInfo.gameObject.SetActive(true);

        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        waveIndex++;
        enemiesSpawned = 0;
        killCount = 0;
        UpdateHUD();
        
        //修改人物属性 包括 ui 
        FindFirstObjectByType<PlayerSpellController>().SetCharacter(ClassManager.Instance.GetClassStats("mage", waveIndex));
        GameManager.Instance.player.GetComponent<PlayerController>().NextLevel();

        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3;
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1);
            GameManager.Instance.countdown--;
        }

        GameManager.Instance.state = GameManager.GameState.INWAVE;

        var level = GameManager.Instance.LevelMap[GameManager.SelectedLevelName];
        foreach (var spawn in level.spawns)
        {
            var baseStats = GameManager.Instance.GetEnemyInfo(spawn.enemy);
            var vars = new Dictionary<string, int> {
                { "wave", waveIndex },
                { "base", baseStats.hp }
            };

            int count = RPNEvaluator.Evaluate(spawn.count ?? "1", vars);
            int[] sequence = spawn.sequence != null && spawn.sequence.Length > 0 ? spawn.sequence : new int[] { 1 };
            float delay = spawn.delay > 0 ? spawn.delay : 2f;
            int spawned = 0, seqIndex = 0;

            while (spawned < count)
            {
                int toSpawn = Mathf.Min(sequence[seqIndex], count - spawned);
                for (int i = 0; i < toSpawn; i++)
                {
                    yield return SpawnEnemy(spawn, waveIndex);
                    enemiesSpawned++;
                    UpdateHUD();
                }
                spawned += toSpawn;
                seqIndex = (seqIndex + 1) % sequence.Length;
                yield return new WaitForSeconds(delay);
                
            }
        }

        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);
        

        levelFinished = (waveIndex >= maxWaves);
        UpdateHUD();

        if (levelFinished && waveButtonLabel)
            waveButtonLabel.text = "Next Level";

        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        if (rewardScreenPanel)
        {
            rewardScreenPanel.SetActive(true);
            
            // EventCenter.Broadcast(EventDefine.ShowSpellPart);
            bool isSpell = Random.Range(0, 2) == 0; // 50%概率选法术50%概率选修正
            
            if (isSpell)
            {
                EventCenter.Broadcast(EventDefine.ShowSpellPart);
            }
            else
            {
                EventCenter.Broadcast(EventDefine.ShowModifierPart);
            }
           
        }

       
    }

    IEnumerator SpawnEnemy(SpawnInfo spawn, int wave)
    {
        var info = GameManager.Instance.GetEnemyInfo(spawn.enemy);
        var vars = new Dictionary<string, int> {
            { "base", info.hp },
            { "wave", wave },
            { "base_speed", (int)info.speed },
            { "base_damage", info.damage }
        };

        int hp     = RPNEvaluator.Evaluate(spawn.hp     ?? "base", vars);
        int speed  = RPNEvaluator.Evaluate(spawn.speed  ?? "base_speed", vars);
        int damage = RPNEvaluator.Evaluate(spawn.damage ?? "base_damage", vars);
        Debug.Log($"Spawning {spawn.enemy} | Wave: {wave} | HP: {hp}, Speed: {speed}, Damage: {damage}");
        var sp = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        var pos = sp.transform.position + (Vector3)Random.insideUnitCircle * 1.8f;

        var e = Instantiate(enemyPrefab, pos, Quaternion.identity);
        e.GetComponent<SpriteRenderer>().sprite =
            GameManager.Instance.enemySpriteManager.Get(info.sprite);

        var ec = e.GetComponent<EnemyController>();

        // 用组件而不是 new 构造
        var hittable = e.GetComponent<Hittable>();
        if (hittable == null) hittable = e.AddComponent<Hittable>();
        // 初始化属性
        hittable.max_hp = hp;
        hittable.hp = hp;
        hittable.team = Hittable.Team.MONSTERS;
        hittable.owner = e;
        ec.hp = hittable;
        ec.speed = speed;
        ec.damage = damage;
        ec.maxHP = ec.currentHP = hp;

        ec.hp.OnDeath += () => {
            killCount++;
            UpdateHUD();
        };

        GameManager.Instance.AddEnemy(e);
        yield return new WaitForSeconds(0.5f);
    }

    void UpdateHUD()
    {
        if (!HUDInfo) return;

        string lvl = GameManager.SelectedLevelName;
        string waveLine = (maxWaves == int.MaxValue)
            ? $"Wave {waveIndex}"
            : $"Wave {waveIndex} / {maxWaves}";

        HUDInfo.text = $"Level: {lvl}\n{waveLine}\nKills: {killCount}";
    }

    public void NextWave()
    {
        if (rewardScreenPanel) 
        { 
            rewardScreenPanel.SetActive(false);
            EventCenter.Broadcast(EventDefine.CloseSpellPart);
            EventCenter.Broadcast(EventDefine.CloseModifierPart);
        }
        if (levelFinished)
        {
            if (GameManager.SelectedLevelName == "Endless")
            {
                level_selector.gameObject.SetActive(true);
                return;
            }

            string next = (GameManager.SelectedLevelName == "Easy") ? "Medium" : "Endless";
            StartLevel(next);
        }
        else
        {
            
            StartCoroutine(SpawnWave());
          
            
        }
    }
    
    //按钮点击更新法术 
    
}
