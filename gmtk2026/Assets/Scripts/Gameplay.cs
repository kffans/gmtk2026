using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Gameplay : MonoBehaviour
{
    
    private KeyCode[] keypadCodes = new KeyCode[] {
        KeyCode.Alpha0,
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9
    };
    
    private float  TOTAL_TIME      = 0;

    private int fontSize = 36;
    
    private int xPosTextModule = -850;
    private int yPosTextModule = -70;
    
    private Dial.State state = null;
    
    public Transform framesObj;
    public GameObject frameObj;
    
    private bool dialogueFinish = false;
    
    public enum GameState { NONE, INTRO, MAIN_MENU, DIALOGUE, BREAK, CREDITS }
    private GameState gameState = GameState.INTRO;
    
    private bool haveTextsChanged = true;
    private float scrollValue = 0;
    
    private bool triggerOnce = true;
    
    public AudioSource audioSource;
    //public AudioClip introClip;
    
    public GameObject dialogueText;
    public Transform dialogueTexts;
    
    private float breakTime = 0.0f;
    private float breakTimeTotal = 4.0f;
    
    public GameObject dialogueObj;
    public GameObject breakObj;
    
    private List<GameObject> texts = null;
    
   
    void Start () {

    }

    void Update () {
        TOTAL_TIME += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
        }
        
        switch (gameState) {
            case GameState.INTRO: {
                triggerOnce = true;
                Dial.DIAL_DEFAULT_TEXT_WIDTH = 40;
                gameState = GameState.DIALOGUE;
                Dial.SetString("NextDialogue", "intro");   
                state = Dial.State_I(Dial.GetString("NextDialogue"));
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
                        haveTextsChanged = true;
                    }
                }
                if (Dial.IsCurrentStatus(state, Dial.Status.WAIT_FOR_CHOICE)) {
                    int choices_s = Dial.GetChoicesSize(state);
                    for (int i = 0; i < choices_s; i++) {
                        /* @TODO checks here if the option is clicked with mouse */
                        if (Input.GetKeyUp(keypadCodes[i + 1])) {
                            bool isValid = Dial.IsChoiceValid(state, i);
                            if (!isValid) { continue; }
                            Dial.Choice(state, i);
                            haveTextsChanged = true;
                            break;
                        }
                    }
                }
                if (Dial.IsCurrentStatus(state, Dial.Status.FINISHED)) {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {
                        dialogueFinish = true;
                    }
                    if (dialogueFinish) {
                        dialogueFinish = false;
                        //dialogueObj.SetActive(false);
                        if (Dial.GetString("NextDialogue") == "credits") {
                            gameState = GameState.CREDITS;
                        }
                        else {
                            gameState = GameState.BREAK;
                        }
                        // @TODO reset texts?
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
                        //textRect = rect.Rect_I(xPosTextModule, state.textObjs[i].yPos + yPosTextModule, xPosTextModule + widthTextModule, (state.textObjs[i].yPos + state.textObjs[i].height) + yPosTextModule); // CS
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
                    
                    //Dial.SetString("NextDialogue", "");
                    state = Dial.State_I(Dial.GetString("NextDialogue"));
                    
                    breakObj.SetActive(true);
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
