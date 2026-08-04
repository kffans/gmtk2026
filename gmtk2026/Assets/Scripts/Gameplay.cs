using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 

public class Gameplay : MonoBehaviour
{
    private KeyCode[] keypadCodes = new KeyCode[] {
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
    };
    
    private float TOTAL_TIME = 0;
    private int fontSize = 36;
    private int xPosTextModule = -850;
    private int yPosTextModule = -70;
    
    private Dial.State state = null;
    
    public Transform framesObj;
    public GameObject frameObj;
    
    private bool dialogueFinish = false;
    
    public enum GameState { NONE, INTRO, MAIN_MENU, DIALOGUE, BREAK, CREDITS }
    public GameState gameState = GameState.INTRO;
    
    private bool haveTextsChanged = true;
    private float scrollValue = 0;
    private bool triggerOnce = true;
    
    public GameObject dialogueText;
    public Transform dialogueTexts;
    
    private float breakTime = 0.0f;
    private float breakTimeTotal = 4.0f;
    
    public GameObject dialogueObj;
    public GameObject breakObj;
    
    private List<GameObject> texts = null;
    public List<string> availableDays;

    private bool deathEventTriggered = false;
    private bool creditsEventTriggered = false;
    /// <summary>
    /// Variables for handling the mana system
    /// </summary>
    public enum HealthLevel { ItsOver = 0, ItsBad = 1, CouldBeBetter = 2, Nice = 3 }
    public enum StressLevel { CannotNoMore = 0, MustBeTough = 1, Struggle = 2, Breeze = 3 }
    public enum RelationsLevel { FamilyNoMore = 0, CloseToEnd = 1, Tolerate = 2, Loving = 3 }

    public HealthLevel currentHealth = HealthLevel.Nice;
    public StressLevel currentStress = StressLevel.Breeze;
    public RelationsLevel currentRelations = RelationsLevel.Loving;
    public int money = 100;
    public int currentDay = 0;

    /// <summary>
    /// For displaying current states of variables
    /// </summary>
    public TextMeshProUGUI stressTextUI; 
    public TextMeshProUGUI relationsTextUI;
    public TextMeshProUGUI healthTextUI;
    public TextMeshProUGUI dayTextUI;

    [SerializeField] private Transform _moneySpawnContainer;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 0f);
    [SerializeField] private CurrencyDenomination[] _denominations;
    private List<GameObject> _spawnedMoney = new List<GameObject>();

    [System.Serializable]
    public struct CurrencyDenomination
    {
        public int value;
        public GameObject prefab;
    }

    private bool isNextEvening = false;

    private string GetHealthDescription(HealthLevel health)
    {
        switch (health)
        {
            case HealthLevel.ItsOver: return "It's over";
            case HealthLevel.ItsBad: return "It's bad";
            case HealthLevel.CouldBeBetter: return "Could be better";
            case HealthLevel.Nice: return "Nice";
            default: return "Unknown";
        }
    }

    private string GetStressDescription(StressLevel stress)
    {
        switch (stress)
        {
            case StressLevel.CannotNoMore: return "I cannot no more";
            case StressLevel.MustBeTough: return "I must be tough";
            case StressLevel.Struggle: return "Sometimes I struggle";
            case StressLevel.Breeze: return "It's a breeze";
            default: return "Unknown";
        }
    }

    private string GetRelationsDescription(RelationsLevel relations)
    {
        switch (relations)
        {
            case RelationsLevel.FamilyNoMore: return "Family no more";
            case RelationsLevel.CloseToEnd: return "Close to end";
            case RelationsLevel.Tolerate: return "They tolerate me";
            case RelationsLevel.Loving: return "They loving me";
            default: return "Unknown";
        }
    }

    private void UpdateUI()
    {
        if (healthTextUI != null) healthTextUI.text = "Health: " + GetHealthDescription(currentHealth);
        if (stressTextUI != null) stressTextUI.text = "Stress: " + GetStressDescription(currentStress);
        if (relationsTextUI != null) relationsTextUI.text = "Relations: " + GetRelationsDescription(currentRelations);
        if (dayTextUI != null) dayTextUI.text = "Day: " + currentDay;
        
        GenerateMoney(money);
    }

    private void GenerateMoney(int targetAmount)
    {
        ClearPreviousMoney();

        int currentAmount = targetAmount;
        int spawnIndex = 0; 

        foreach (var denomination in _denominations)
        {
            if (denomination.value <= 0) continue;

            while (currentAmount >= denomination.value)
            {
                currentAmount -= denomination.value;
                SpawnGeldPrefab(denomination.prefab, spawnIndex);
                spawnIndex++;
            }
        }
    }

    private void SpawnGeldPrefab(GameObject prefab, int index)
    {
        if (prefab == null) return;

        GameObject newMoney = Instantiate(prefab, _moneySpawnContainer, false);
        
        RectTransform rectTransform = newMoney.GetComponent<RectTransform>();
        if (rectTransform != null) {
            rectTransform.anchoredPosition = new Vector2(index * _spacing.x, index * _spacing.y);
        } else {
            newMoney.transform.localPosition = new Vector3(index * _spacing.x, index * _spacing.y, 0);
        }

        _spawnedMoney.Add(newMoney);
    }

    private void ClearPreviousMoney()
    {
        foreach (var moneyGo in _spawnedMoney)
        {
            if (moneyGo != null)
            {
                Destroy(moneyGo);
            }
        }
        _spawnedMoney.Clear();
    }

    private void PushVariablesToDial()
    {
        Dial.SetInt("Health", (int)currentHealth);
        Dial.SetInt("Stress", (int)currentStress);
        Dial.SetInt("Relations", (int)currentRelations);
        Dial.SetInt("Day", currentDay);
        Dial.SetInt("Money", money);
    }

    private void PullVariablesFromDial()
    {
        HealthLevel newHealth = (HealthLevel)Dial.GetInt("Health");
        StressLevel newStress = (StressLevel)Dial.GetInt("Stress");
        RelationsLevel newRelations = (RelationsLevel)Dial.GetInt("Relations");
        int newDay = Dial.GetInt("Day");
        int newMoney = Dial.GetInt("Money");

        bool statsChanged = (newHealth != currentHealth || newStress != currentStress || newRelations != currentRelations);
        bool moneyChanged = (newMoney != money);

        currentHealth = newHealth;
        currentStress = newStress;
        currentRelations = newRelations;
        money = newMoney;
        currentDay = newDay;    

        
        if (statsChanged) AudioManager.Instance.PlaySFX("statChange");
        if (moneyChanged) AudioManager.Instance.PlaySFX("moneyChange");

        UpdateUI();
    }
    
    void Start () 
    {
        if (_denominations != null && _denominations.Length > 0)
        {
            _denominations = _denominations.OrderByDescending(d => d.value).ToArray();
        }

        UpdateUI();
    }

    void Update () {
        TOTAL_TIME += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
        }
        
        switch (gameState) {
            case GameState.INTRO: {
                PushVariablesToDial();
                triggerOnce = true;
                Dial.DIAL_DEFAULT_TEXT_WIDTH = 40;
                gameState = GameState.DIALOGUE;
                Dial.SetString("NextDialogue", "intro");   
                state = Dial.State_I(Dial.GetString("NextDialogue"));
                state.flags = Dial.Flags.IGNORE_ACTOR_NAME;
                break;
            }
            case GameState.DIALOGUE: {
                if (triggerOnce) {
                    triggerOnce = false;
                }
                
                /* keyboard events */
                if (Dial.IsCurrentStatus(state, Dial.Status.WAIT_FOR_CONTINUATION)) {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {
                        Dial.Continuation(state);

                        AudioManager.Instance.PlaySFX("dialogueClick");

                        haveTextsChanged = true;
                        PullVariablesFromDial();
                    }
                }
                if (Dial.IsCurrentStatus(state, Dial.Status.WAIT_FOR_CHOICE)) {
                    int choices_s = Dial.GetChoicesSize(state);
                    for (int i = 0; i < choices_s; i++) {
                        if (Input.GetKeyUp(keypadCodes[i + 1])) {
                            bool isValid = Dial.IsChoiceValid(state, i);
                            if (!isValid) { continue; }
                            
                            AudioManager.Instance.PlaySFX("dialogueClick");

                            Dial.Choice(state, i);
                            PullVariablesFromDial();
                            haveTextsChanged = true;
                            break;
                        }
                    }
                }
                if (Dial.IsCurrentStatus(state, Dial.Status.FINISHED)) {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {
                        dialogueFinish = true;
                    }
                    if (dialogueFinish) 
                    {
                        dialogueFinish = false;
                        
                        bool isStatsCritical = (int)currentHealth <= 1 || (int)currentStress <= 1 || (int)currentRelations <= 1;

                        if (isStatsCritical && !deathEventTriggered)
                        {
                            deathEventTriggered = true;
                            Dial.SetString("NextDialogue", "death");
                            gameState = GameState.BREAK;
                        }
                        else if (Dial.GetString("NextDialogue") == "credits" && !creditsEventTriggered) 
                        {
                            creditsEventTriggered = true;
                            gameState = GameState.CREDITS;
                        }
                        else if (isNextEvening) 
                        {
                            Dial.SetString("NextDialogue", "evening"); 
                            isNextEvening = false;
                            gameState = GameState.BREAK;
                        }
                        else if (availableDays != null && availableDays.Count > 0) 
                        {

                            currentDay++; 
                            PushVariablesToDial();
                            UpdateUI();

                            int randomIndex = Random.Range(0, availableDays.Count);
                            string chosenDay = availableDays[randomIndex];
                            
                            availableDays.RemoveAt(randomIndex); 
                            
                            Dial.SetString("NextDialogue", chosenDay);
                            isNextEvening = true; 
                            gameState = GameState.BREAK;
                        }
                        else if (!creditsEventTriggered) 
                        {
                            creditsEventTriggered = true;
                            Dial.SetString("NextDialogue", "credits");
                            gameState = GameState.BREAK;
                        }
                        else
                        {
                            gameState = GameState.CREDITS;
                        }
                        
                        haveTextsChanged = true;
                        triggerOnce = true;
                        break;
                    }
                }
                if (state != null) { // @CS
                    if (state.shouldChangeFrame == true) {
                        state.shouldChangeFrame = false;
                        Texture frameTexture = Resources.Load("ImgContent/" + state.frames[0]) as Texture;
                        GameObject frame = Instantiate(frameObj, framesObj);
                        frame.GetComponent<RawImage>().texture = frameTexture;
                        LeanTween.moveY(frame, frame.transform.position.y - 528f, 0.35f)
                            .setEase(LeanTweenType.easeOutBack)
                            .setOnComplete(() =>
                            {
                                if (state.frames.Count != 0) {
                                    state.frames.RemoveAt(0);
                                }
                                if (state.frames.Count != 0) {
                                    state.shouldChangeFrame = true;
                                }
                            });
                    }
                    
                    if (framesObj.childCount > 10) {
                        Destroy(framesObj.GetChild(0).gameObject);
                    }
                    
                    List<GameObject> texts = new List<GameObject>();
                    
                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    
                    scrollValue += scroll * 90.0f;
                    if (scrollValue < 0.0f) {
                        scrollValue = 0.0f;
                    }
                    
                    if (haveTextsChanged) {
                        haveTextsChanged = false;
                        
                        scrollValue = 0;
                        float currentHeight = 0.0f;
                        float totalHeight = 0.0f;
                        while (Dial.GetTextsSize(state) > 15) {
                            state.textObjs.RemoveAt(0);
                        }
                        for (int i = 0; i < Dial.GetTextsSize(state); i++) { // update height of text objects and calculate total height
                            GameObject temp = Instantiate(dialogueText, dialogueTexts);
                            TextMeshProUGUI tempText = temp.GetComponent<TextMeshProUGUI>();
                            tempText.text = Dial.GetText(state, i);
                            tempText.ForceMeshUpdate();
                            state.textObjs[i].height = -tempText.bounds.size.y - fontSize + 0;
                            totalHeight += state.textObjs[i].height;
                            Destroy(temp);
                        }
                        for (int i = 0; i < Dial.GetTextsSize(state); i++) { // calculate destination position
                            state.textObjs[i].yDestPos = currentHeight - totalHeight;
                            currentHeight += state.textObjs[i].height;
                        }
                    }
                    
                    for (int i = dialogueTexts.childCount - 1; i >= 0; i--) {
                        Destroy(dialogueTexts.GetChild(i).gameObject);
                    }
                    
                    for (int i = 0; i < Dial.GetTextsSize(state); i++) { // create texts and set color
                        GameObject text = Instantiate(dialogueText, dialogueTexts);
                        text.GetComponent<TextMeshProUGUI>().text = Dial.GetText(state, i);
                        texts.Add(text);                            
                    }
                    for (int i = 0; i < texts.Count; i++) { // changing position of each text
                        float moveValue = (state.textObjs[i].yDestPos - scrollValue - state.textObjs[i].yPos);
                        if (moveValue > 0.5f)  state.textObjs[i].yPos += Mathf.Sqrt(Mathf.Abs(moveValue));
                        if (moveValue < -0.5f) state.textObjs[i].yPos -= Mathf.Sqrt(Mathf.Abs(moveValue));
                        texts[i].transform.position = new Vector3(xPosTextModule, state.textObjs[i].yPos + yPosTextModule, 0.0f); // CS
                    }
                    texts.Clear();
                }
                break;
            }
            case GameState.BREAK: {
               
                if (triggerOnce) {
                    triggerOnce = false;
                    texts = new List<GameObject>();
                    
                    state = Dial.State_I(Dial.GetString("NextDialogue"));
                    
                    breakObj.SetActive(true);

                    AudioManager.Instance.PlaySFX("interlude");
                }
                breakTime += Time.deltaTime;
                
                if (breakTime >= breakTimeTotal) {
                    breakTime = 0;
                    gameState = GameState.DIALOGUE;
                    breakObj.SetActive(false);
                    triggerOnce = true;
                }
                break;
            }
            default: break;
        }
    }
}