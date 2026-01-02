using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using UnityEngine.UI;
using System;
using UnityEngine.SocialPlatforms.Impl;
public enum GameState
{
    WaitingForInput,
    Animating,
}

public enum medicationType
{
    RedBlue,
    DiagonalGrey,
    OrangeMatte,
    SnowCrystal,
    EnderPearl
}
public class Affliction
{
    public string name;
    public Dictionary<medicationType, int> expectedMedicationCount;
    public Affliction(string name , Dictionary<medicationType, int> expectedMedCounts) 
    {
        this.name = name;
        expectedMedicationCount = expectedMedCounts;
    }
}

public class GameManager : MonoBehaviour
{
    public float animationLockTime = 1f;
    public GameObject spawners;
    // sprites
    public Sprite redBlue;
    public Sprite diagonalGrey;
    public Sprite orangeMatte;   
    public Sprite snowCrystal;
    public Sprite enderPearl;

    // text
    public Text redBlueText;
    public Text diagonalGreyText;
    public Text orangeMatteText;   
    public Text snowCrystalText;
    public Text enderPearlText;

    public Text timerText;
    public Text scoreText;

    // Audio
    public AudioSource sfxSource;
    public AudioClip pillFallClip, successClip, failClip;

    public GameOver gameOverManager;
    // hard coded
    private List<Affliction> afflictions = new List<Affliction>();
    private int currentScore = 0;
    private Dictionary<medicationType, int> expectedMedications = new Dictionary<medicationType, int>();
    // what's present in the game, (actual (this) vs expected)
    private Dictionary<medicationType, int> actualMedications = new Dictionary<medicationType, int>();
    private Dictionary<Affliction, int> currentTrackedAfflictions = new Dictionary<Affliction, int>();
    private bool shouldPass;
    private GameState currentGameState = GameState.WaitingForInput;
    private List<GameObject> pillObjects = new List<GameObject>();
    private Dictionary<medicationType, Text> textUI = new Dictionary<medicationType, Text>();
    private float gameDuration = 60f;
    private float timer = 5f;
    private bool gameEnded = false;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Assign all Afflictions
        Affliction fever = new Affliction("Fever", new Dictionary<medicationType, int>{{medicationType.RedBlue, 2}});
        Affliction ed = new Affliction("ED", new Dictionary<medicationType, int>{
            {medicationType.DiagonalGrey, 1}, 
            {medicationType.OrangeMatte, 1}
        });
        Affliction hangover = new Affliction("Hangover", new Dictionary<medicationType, int>
        {
           {medicationType.DiagonalGrey, 1},
           {medicationType.SnowCrystal, 1} ,
           {medicationType.OrangeMatte, 1}
        });
        Affliction obseity = new Affliction("Obseity", new Dictionary<medicationType, int>
        {
           {medicationType.RedBlue, 3},
           {medicationType.OrangeMatte, 2} 
        });
        Affliction depression = new Affliction("Depression", new Dictionary<medicationType, int>
        {
            {medicationType.RedBlue, 1},
            {medicationType.EnderPearl, 1}
        });
        Affliction hypothermia = new Affliction("Hypothermia", new Dictionary<medicationType, int>
        {
            {medicationType.SnowCrystal, 2},
            {medicationType.EnderPearl, 1}
        });

        afflictions.Add(fever);
        afflictions.Add(ed);
        afflictions.Add(hangover);
        afflictions.Add(obseity);
        afflictions.Add(depression);
        afflictions.Add(hypothermia);

        currentTrackedAfflictions[fever] = 0;
        currentTrackedAfflictions[ed] = 0;
        currentTrackedAfflictions[hangover] = 0;
        currentTrackedAfflictions[obseity] = 0;
        currentTrackedAfflictions[depression] = 0;
        currentTrackedAfflictions[hypothermia] = 0;

        timer = gameDuration;

        // Spawner discovery
        foreach (Transform column in spawners.transform)
        {
            // 2️⃣ Get cells in each column
            foreach (Transform child in column)
            {
                
                pillObjects.Add(child.GetChild(0).gameObject);
            }
        }

        expectedMedications[medicationType.RedBlue] = 0;
        expectedMedications[medicationType.DiagonalGrey] = 0;
        expectedMedications[medicationType.OrangeMatte] = 0;
        expectedMedications[medicationType.SnowCrystal] = 0;
        expectedMedications[medicationType.EnderPearl] = 0;

        // UI Stuff
        textUI[medicationType.RedBlue] = redBlueText;
        textUI[medicationType.DiagonalGrey] = diagonalGreyText;
        textUI[medicationType.OrangeMatte] = orangeMatteText;
        textUI[medicationType.SnowCrystal] = snowCrystalText;
        textUI[medicationType.EnderPearl] = enderPearlText;

        StartRound();
    }

    // Update is called once per frame
    void Update()
    {

        // Check Flag to see if animation is playing
        // ----------------------------------------------
        // Get Player Input
        // If Player Input is Left, process Pass
        // If Plyaer Input is Right, process Deny
        // Start new Round 
        // ----------------------------------------------\

        // check if game is over
        if (gameEnded)
        {
            // Switoch to game over screen
            return;
        }
        
        timer -= Time.deltaTime;
        // Update timer UI
        timerText.text = Math.Floor(timer).ToString();
        if (timer <= 0)
        {
            EndGame();
            return;
        }


        if (currentGameState != GameState.WaitingForInput)
        {
            return;
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            HandlePlayerChoice(playerPassed: true);
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            HandlePlayerChoice(playerPassed: false);
        }
    }

    private void EndGame()
    {
        gameEnded = true;
        gameOverManager.ShowGameOver(currentScore);
    }

    private void HandlePlayerChoice(bool playerPassed)
    {

        if (playerPassed && shouldPass || !playerPassed && !shouldPass)
        {
            // Play song 
            sfxSource.PlayOneShot(successClip);

            currentScore += 1;
        } else
        {
            sfxSource.PlayOneShot(failClip);
            currentScore -= 1;
        }
        
        scoreText.text = currentScore.ToString();

        EndRound();
        StartRound();
        // POSTMVP dynamic scoring (streaking)
    }

    private void StartRound()
    {
        // Get Random assortment of Diseases and break those up in med requirements
        SetExpectedMedication();
        // Set Actual Medication
        SetActualMedications();
        // Render the pills
        RenderPills();
        // Start animation + wait for seconds
        StartCoroutine(AnimationLockCoroutine());
        // Start the actual animation...
    }

    private void RenderPills()
    {
        // randomly choose a cell
        // for each pill 
        foreach (KeyValuePair<medicationType, int> pillCount in actualMedications)
        {
            for (int i = 0; i < pillCount.Value; i++)
            {
                // choose a random cell
                int randomIndex = UnityEngine.Random.Range(0, pillObjects.Count);
                // get and render pill if cell does not already have a pill 
                if (pillObjects[randomIndex].activeSelf)
                {
                    i--; 
                    continue;
                } else
                {
                    SpriteRenderer sp = pillObjects[randomIndex].GetComponent<SpriteRenderer>();
                    // change the pill image
                    switch (pillCount.Key)
                    {
                        case medicationType.RedBlue:
                            sp.sprite = redBlue;
                            break;
                        case medicationType.DiagonalGrey:
                            sp.sprite = diagonalGrey;
                            break;
                        case medicationType.OrangeMatte:
                            sp.sprite = orangeMatte;
                            break;
                        case medicationType.SnowCrystal:
                            sp.sprite = snowCrystal;
                            break;
                        case medicationType.EnderPearl:
                            sp.sprite = enderPearl;
                            break;
                        default:
                            Debug.Log("WTF");
                            break;
                    }
                    pillObjects[randomIndex].SetActive(true);
                }
            }             
        }
        // render pill count
        foreach (KeyValuePair<medicationType, int> pillCount in expectedMedications)
        {
            textUI[pillCount.Key].text = pillCount.Value.ToString();
        }
    }

    IEnumerator AnimationLockCoroutine()
    {
        currentGameState = GameState.Animating;

        List<Vector3> targets = new List<Vector3>();
        List<Vector3> startPos = new List<Vector3>();
        foreach (var pillObject in pillObjects)
        {
            targets.Add(pillObject.transform.position);
            // shift each pill object up
            pillObject.transform.position += Vector3.up * 0.20f;
            startPos.Add(pillObject.transform.position);
        }

        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeT = t * t * t * t;

            for (int i = 0; i < pillObjects.Count; i++)
                pillObjects[i].transform.position = Vector3.Lerp(startPos[i], targets[i], easeT);
            
            yield return null;
        }

        // Play audio clip
        sfxSource.PlayOneShot(pillFallClip);

        for (int i = 0; i < pillObjects.Count; i++)
            pillObjects[i].transform.position = targets[i];

        currentGameState = GameState.WaitingForInput;
    }

    private void EndRound()
    {
        // Reset currentTrackedAfflictions
        foreach (var key in currentTrackedAfflictions.Keys.ToList())  // use ToList() to avoid "collection modified" issues
        {
            currentTrackedAfflictions[key] = 0;
        }
        // Reset expected and actual medications
        foreach (var key in expectedMedications.Keys.ToList())
        {
            expectedMedications[key] = 0;
        }
        // Reset actual medications
        foreach (var key in actualMedications.Keys.ToList())
        {
            actualMedications[key] = 0;
        }
        // Reset the cells
        foreach (var pillCell in pillObjects)
        {
            pillCell.SetActive(false);
        }
    }

    private void SetActualMedications()
    {
        // Alter values of medication {0, 1, 2, 3} with nominally distribution with a random * -1 (50/50)
        bool isSame = true;
        foreach (var expectedMedication in expectedMedications) // {MedicationType, int}
        {
            int newValue = AlterValue(expectedMedication.Value);
            isSame = isSame && (newValue == expectedMedication.Value);
            actualMedications[expectedMedication.Key] = newValue;
        }

        shouldPass = isSame;
        // POSTMVP alter logic to make it so that when score is higher, value differs by less by differs more
    }

    // Returns list of afflict
    private void SetExpectedMedication()
    {
        
        // Choose randomly 1 affliction
        Affliction currentAffliction = afflictions[UnityEngine.Random.Range(0, afflictions.Count)];
        currentTrackedAfflictions[currentAffliction] = 1;

        // Have a random chance to add another affliction
        if (timer <= 45)
        {
            float chance = UnityEngine.Random.Range(0f, 1f);
            if (chance < 0.5f)
            {
                Affliction plusAffliction = afflictions[UnityEngine.Random.Range(0, afflictions.Count)];
                currentTrackedAfflictions[plusAffliction] += 1;
            }
        }

        if (timer <= 20)
        {
            float chance = UnityEngine.Random.Range(0f, 1f);
            if (chance < 0.5f)
            {
                Affliction plusAffliction = afflictions[UnityEngine.Random.Range(0, afflictions.Count)];
                currentTrackedAfflictions[plusAffliction] += 1;
            }
        }

        // set expected medications
        foreach (var affliction in currentTrackedAfflictions) // this is {affliction, multipler (day)}
        {
            // Debug.Log("this is my affliction value ( dayts sicks) " + affliction.Value);
            foreach (var medicationReq in affliction.Key.expectedMedicationCount) // this is { medicationType, count }
            {
                // Debug.Log("this is the medication req for " + medicationReq.Key + " and is " + medicationReq.Value);
                expectedMedications[medicationReq.Key] += medicationReq.Value * affliction.Value;
            }
        }

        // POST MVP: Dynamically choose affliction based on current score
        // POST MVP: Randomly choose number of days an affliction should be present for
    }


    // UTILs
    private int AlterValue(int value)
    {
        // Step 1: Decide if we increase or decrease (50/50)
        int sign = UnityEngine.Random.value < 0.5f ? -1 : 1;

        // Step 2: Roll for magnitude change
        float roll = UnityEngine.Random.value; // 0.0 - 1.0

        int change = 0;
        if (roll < 0.90f)
            change = 0;       // 70%
        else if (roll < 0.95f)
            change = 1;       // 15%
        else if (roll < 0.98f)
            change = 2;       // 10%
        else
            change = 3;       // 5%

        // Apply increase/decrease
        value += change * sign;

        return System.Math.Max(value, 0);
    }
}
