using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Dial {
    
    /* Constants */
    public const int DIAL_JUMP_LOOP_LIMIT = 100;
    
    public static int DIAL_DEFAULT_TEXT_WIDTH = 40;

    public enum TextType {
        NONE,
        NORMAL,
        CHOICE_NORMAL,
        CHOICE_SELECTED,
        CHOICE_ACCENTED,
        CHOICE_CHANCE
    }
    

    public class TextObject {
        public string actor_n;
        public string text;
        public TextType type;
        public float yPos;
        public float yDestPos;
        public float height;
    }

    public class Pos {
        public int text_i;
        public int condNestingDepth;
        public Pos (int text_i = 0, int condNestingDepth = 0) {
            this.text_i = text_i;
            this.condNestingDepth = condNestingDepth;
        }
    }

    public class ChoiceObject {
        public string instrText;
        public string displayText;
        public TextType type;
        public Dictionary<string,string> accentedOptions;
        public Pos jumpPos;
    }
    
    public enum Status {
        NONE,
        WAIT_FOR_CONTINUATION,
        WAIT_FOR_CHOICE,
        INTERPRET,
        FATAL_ERROR,
        FINISHED
    }
    
    [Flags]
    public enum Flags {
        NONE = 0,
        LEAVE_PREFIX_AFTER_CHOICE = 1 << 0
    }
    
    public class Pair {
        public int Item1;
        public string Item2;
        public Pair (int Item1 = 0, string Item2 = "") {
            this.Item1 = Item1;
            this.Item2 = Item2;
        }
    }

    public class State {
        public string text; // @CS
        public int text_s;
        public int textWidth;
        public string displayText;
        public string actor_n;
        public List<TextObject> textObjs;
        public Status status;
        public Flags flags;
        public Pos currentPos;
        public Dictionary<int, Pos> jumpBasePos;
        public List<(int,Pos)> jumpHistory;
        public List<(string,string)> persCond;
        public List<bool> condElse;
        public Dictionary<string,Pair> localVars;
        public List<ChoiceObject> choices;
        public IEnumerator<string> currentAccent;
        public SortedSet<string> possibleAccents;
        public Dictionary<int, bool> hasOneUseChoiceRecurred;
        public Dictionary<int, int> condRepeat_c;
        public int seedRandom;
        public System.Random random;
        public Dictionary<string, Pair> globalVarsCopy;
        public List<string> saveData;
        
        public bool shouldChangeFrame;
        public List<string> frames;
    }


    public static Dictionary<string, Pair> Vars = new Dictionary<string, Pair>(); /* global variables */



    public static int stringToInt (string varText, ref bool hasSucceeded) { /* tries to convert string to int and shows whether the conversion succeeded */
        int converted = 0;
        if (Int32.TryParse(varText, out converted)){
            hasSucceeded = true;
            return converted;
        }
        else {
            hasSucceeded = false;
            return 0;
        }
    }

// // // ERROR-HANDLING FUNCTIONS // // //

    public static string GetCurrentTextFilePos (string txt, int target_i) {
        int line_c = 1; /* current line */
        int col_c = 0;  /* current column */
        for (int i = 0; i != target_i; i++) {
            if (txt[(int)i] == '\n') {
                line_c++;
                col_c = 0;
            }
            col_c++;
        }
        return "(Line:" + line_c.ToString() + ", Col:" + col_c.ToString() + ")";
    }

    public static void Error (string errText, string txt, int t_i) {
        Debug.LogError("\n:ERROR: " + GetCurrentTextFilePos(txt, t_i) + " " + errText + "\n\n");
        //#ifdef DIAL_DEBUG
        //std::cerr<<"\n:ERROR: "<<GetCurrentTextFilePos(txt, ref t_i)<<" "<<errText<<"\n\n";
        //#endif
        //#ifndef DIAL_DEBUG
        /* @TODO if not DIAL_DEBUG, then could save the errors to a log file */
        //#endif
    }

    public static void Error (string errText) {
        Debug.LogError("\n:ERROR: " + errText + "\n\n");
        //#ifdef DIAL_DEBUG
        //std::cerr<<"\n:ERROR: "<<errText<<"\n\n";
        //#endif
        //#ifndef DIAL_DEBUG
        /* @TODO if not DIAL_DEBUG, then could save the errors to a log file */
        //#endif
    }

    //#ifdef DIAL_DEBUG
    //List<State> backtrack;
    //map<string, ValueTuple<int,string>> backtrackVars;
    //bool isBacktrackLocked = false;
    public static void SaveBacktrackState (State state) {
        if (state == null) { return; }

        /*
        State backtrackStateSave;

        backtrackStateSave.displayText = "";
        backtrackStateSave.currentPos = state.currentPos;
        backtrackStateSave.condElse = state.condElse;
        backtrackStateSave.hasOneUseChoiceRecurred = state.hasOneUseChoiceRecurred;
        backtrackStateSave.backtrackVars = Vars;

        state.backtrack.Add(backtrackStateSave);
        */
    }

    public static void LoadBacktrackState (State state) {
        if (state == null) { return; }
        
        /*
        if (state.backtrack.size() > 1) {
            state.backtrack.pop_back();
            State backtrack = state.backtrack.back();

            state.displayText = "";
            state.status = Status.INTERPRET;
            state.currentPos = backtrack.currentPos;
            state.condElse = backtrack.condElse;
            state.hasOneUseChoiceRecurred = backtrack.hasOneUseChoiceRecurred;
            Vars = backtrack.backtrackVars;

            state.backtrack.pop_back();
        }
        */
    }
    //#endif
    
    public class SpecChar {
        public bool isStatementOpened;
        public int count;
        public int lockedOut_c;
        public int pos_i;
    }

    public static bool HasDetectedCriticalErrors (State state) {
        if (state == null) { return false; }

        string txt = state.text;
        ref int t_i = ref state.currentPos.text_i;
        bool result = false;

        Dictionary<char, SpecChar> chars = new Dictionary<char, SpecChar>();
        chars.Add('#', new SpecChar());
        chars.Add('@', new SpecChar());
        chars.Add('[', new SpecChar());
        chars.Add(']', new SpecChar());
        chars.Add('{', new SpecChar());
        chars.Add('}', new SpecChar());
        chars.Add('&', new SpecChar());
        chars.Add('|', new SpecChar());
        chars.Add('~', new SpecChar());
        while (t_i != state.text_s) {
            switch (txt[t_i]) {
                case '#': { /* @&{}| */
                    chars['#'].isStatementOpened = !chars['#'].isStatementOpened;
                    chars['#'].count++;
                    chars['#'].pos_i = t_i;
                    break;
                }
                case '@': { /* #&{}| */
                    chars['@'].isStatementOpened = !chars['@'].isStatementOpened;
                    chars['@'].count++;
                    chars['@'].pos_i = t_i;
                    break;
                }
                case '[': {
                    chars['['].isStatementOpened = true;
                    chars['['].pos_i = t_i;
                    break;
                }
                case ']': chars['['].isStatementOpened = false; break;
                case '{': {
                    chars['{'].isStatementOpened = true;
                    chars['{'].count++;
                    chars['{'].pos_i = t_i;
                    break;
                }
                case '}': {
                    chars['{'].isStatementOpened = false;
                    chars['}'].count++;
                    chars['}'].pos_i = t_i;
                    break;
                }
                case '&': { /* #&| */
                    chars['&'].isStatementOpened = !chars['&'].isStatementOpened;
                    chars['&'].count++;
                    chars['&'].pos_i = t_i;
                    break;
                }
                case '|': {
                    if (txt[t_i + 1] == '~') {
                        chars['~'].count++;
                        goto EndOfCounting;
                    }
                    else if (txt[t_i + 1] == '|') {
                        chars['|'].count++;
                        t_i++;
                    }
                    break;
                }
                default: break;
            }
            t_i++;
        }
        EndOfCounting:
        t_i = 0;

        /* @TODO positions in detected errors, where possible */
        if (chars['~'].count == 0)                     { Error("The text doesn't have the file ending symbol '|~'."); result = true; }
        if (chars['&'].isStatementOpened)              { Error("One of the conditional instructions doesn't have a corresponding pair '&'."); result = true; }
        else if (chars['&'].count > chars['|'].count * 2) { Error("There's not enough conditional scope ending '||' symbols"); }
        else if (chars['&'].count < chars['|'].count * 2) { Error("There's too many conditional scope ending '||' symbols"); }
        if (chars['#'].isStatementOpened)              { Error("One of the variable instructions doesn't have a corresponding pair '#'."); result = true; }
        if (chars['@'].isStatementOpened)              { Error("One of the special instructions doesn't have a corresponding pair '@'."); result = true; }
        if (chars['['].isStatementOpened)              { Error("There's an unclosed jump point or jump base and is missing a ']' symbol."); result = true; }
        if (chars['{'].isStatementOpened)              { Error("There's an unclosed choice range and is missing a '}' symbol."); result = true; }
        if (chars['{'].count > chars['}'].count)       { Error("One of choice ranges or choices is not closed with it's corresponding pair '}'."); }
        if (chars['{'].count < chars['}'].count)       { Error("One of choice ranges or choices is not opened with it's corresponding pair '{'."); }
        return result;
    }


    public static void ShowVars (State state) { /* shows all used variables */
        string varsString = "";
        varsString += "\n-------LOCAL--------+\n";
        if (state != null) {
            foreach (var it in state.localVars) {
                if ((it.Value).Item2 == "") {
                    varsString += (it.Key).ToString() + " = " + ((it.Value).Item1).ToString() + "\n";
                }
                else {
                    varsString += (it.Key).ToString() + " = " + ((it.Value).Item2).ToString() + "\n";
                }
            }
        }
        varsString += "\n-------GLOBAL-------+\n";
        foreach (var it in Vars) {
            if ((it.Value).Item2 == "") {
                varsString += (it.Key).ToString() + " = " + ((it.Value).Item1).ToString() + "\n";
            }
            else {
                varsString += (it.Key).ToString() + " = " + ((it.Value).Item2).ToString() + "\n";
            }
        }
        varsString += "--------------------+\n";
        Debug.LogWarning(varsString);
    }
    
    public static void SaveVarDiff (State state) {
        if (state == null) { Error("Couldn't save a difference of variables at the state was deleted."); return; }
        Dictionary<string, Pair> diffVars = new Dictionary<string, Pair>();
        foreach (var variable in Vars) {
            string key = variable.Key;
            var val = variable.Value;

            if (state.globalVarsCopy.ContainsKey(key)) {
                diffVars[key] = val;
                continue;
            }
            else {
                state.globalVarsCopy[key] = new Pair(0, "");
            }
            
            if (state.globalVarsCopy[key] != val) {
                diffVars[key] = val;
            }
        }
        foreach (var pair in diffVars) {
            if ((pair.Value).Item2 != "") {                        
                state.saveData.Add("v:" + pair.Key + " = \"" + (pair.Value).Item2 + "\"");
            }
            else {
                state.saveData.Add("v:" + pair.Key + " = " +  (pair.Value).Item1.ToString());   
            }
        }
        state.globalVarsCopy.Clear();
        foreach (var pair in Vars) {
            state.globalVarsCopy[pair.Key] = pair.Value;
        }
    }

    public static Pair GetVar (State state, string key, bool isNegated) {
        if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); }
        if (state == null) { Error("Local variable was not available as the state was deleted; returning a global variable."); return Vars[key]; }
        bool isNegative = false;
        if (key.Length != 0 && key[0] == '-') {
            isNegative = true;
            key = key.Substring(1);
        }
        if (key.Length != 0) {
            if (!state.condRepeat_c.ContainsKey(state.currentPos.text_i)) { state.condRepeat_c[state.currentPos.text_i] = 0; }
            if (key == "REPEAT")      { Vars[key].Item1 =  state.condRepeat_c[state.currentPos.text_i]; Vars[key].Item2 = ""; }
            if (key == "ONCE")        { Vars[key].Item1 =  (state.condRepeat_c[state.currentPos.text_i] != 0) ? 0 : 1; Vars[key].Item2 = ""; }
            if (key == "TRUE")        { Vars[key].Item1 = 1; Vars[key].Item2 = ""; }
            if (key == "FALSE")       { Vars[key].Item1 = 0; Vars[key].Item2 = ""; }
            if (key == "RANDOM")      { Vars[key].Item1 = state.random.Next(1, 101); Vars[key].Item2 = ""; } /* generates number from 1 to 100*/
            
            if (Char.IsUpper(key, 0)) {
                if (isNegative) {
                    Vars[key].Item1 = (-1) * Vars[key].Item1;
                }
                if (isNegated) {
                    Vars[key].Item1 = (Vars[key].Item1 != 0) ? 0 : 1;                    
                }
                return Vars[key];
            }
            else {
                if (!state.localVars.ContainsKey(key)) { state.localVars[key] = new Pair(0, ""); }
                if (isNegative) {
                    (state.localVars[key]).Item1 = (-1) * (state.localVars[key]).Item1;
                }
                if (isNegated) {
                    (state.localVars[key]).Item1 = ((state.localVars[key]).Item1 != 0) ? 0 : 1;                    
                }
                return state.localVars[key]; 
            }
        }
        else {
            //Error("Could not get a variable as its length is 0.", state.text, state.currentPos.text_i);
        }
        if (!state.localVars.ContainsKey(key)) { state.localVars[key] = new Pair(0, ""); }
        return state.localVars[key];
    }

    public static void LoadJumpBases (State state) {
        if (state == null) { return; }

        string txt = state.text;
        ref int t_i = ref state.currentPos.text_i;
        int condNestingDepth_b = 0;

        while (true) {
            switch (txt[t_i]) {
                case '#': { SeekEndOfStatement(txt, ref t_i, "#"); break; }
                case '@': { SeekEndOfStatement(txt, ref t_i, "@"); break; }
                case '{': { 
                    t_i++;
                    int potentialStartOfChoiceRangePos_i = t_i;
                    SeekUntil(txt, ref t_i, "{}");
                    if (txt[t_i] == '{') {
                        t_i = potentialStartOfChoiceRangePos_i;
                    }
                    else {
                        t_i++;
                    }
                    break;
                }
                case '[': {
                    if (txt[t_i + 1] == '[') { /* [[...]] */
                        t_i++;
                        int beginningOfJumpBaseNumber_i = t_i;
                        string jumpBaseNumberText = ScanTextUntil(txt, ref t_i, "-_ ]"); /* examples: [[20-desc.]] , [[7_the scene]] , [[6 plot branch]] , [[38]] */
                        bool hasSucceeded = false; int jumpBaseNumber = stringToInt(jumpBaseNumberText, ref hasSucceeded);
                        if (hasSucceeded) {
                            t_i = beginningOfJumpBaseNumber_i;
                            SeekUntil(txt, ref t_i, "]");
                            while (txt[t_i] == ']') { /* ]]*... */
                                t_i++;
                            }

                            Pos jumpBase_b = new Pos();
                            jumpBase_b.text_i = t_i;
                            jumpBase_b.condNestingDepth = condNestingDepth_b;
                            state.jumpBasePos[jumpBaseNumber] = jumpBase_b;
                        }
                        else {
                            Error("Following jump base number could not be interpreted: " + jumpBaseNumberText, txt, t_i);
                        }
                    }
                    else { /* [...] */
                        t_i++;
                    }
                    break;
                }
                case '&': {
                    condNestingDepth_b++;
                    SeekEndOfStatement(txt, ref t_i, "&");
                    break;
                }
                case '|': {
                    if (txt[t_i + 1] == '~') { /* |~ */
                        t_i = 0;
                        return;
                    }
                    else if (txt[t_i + 1] == '|') { /* || */
                        condNestingDepth_b--;
                        if (condNestingDepth_b < 0) {
                            Error("Conditional nesting depth is below zero. There are stray '||' symbols.", txt, t_i);
                            condNestingDepth_b = 0;
                        }
                        t_i += 2;
                    }
                    else { /* | */
                        t_i++;
                    }
                    break;
                }
                default: t_i++; break;
            }
        }
    }


    public static bool IsWhitespace (char character) {
        return (character == ' ' || character == '\t' || character == '\n' || character == '\r');
    }

    public static string RemoveWhitespace (string dirtyText) {
        string cleanText = "";
        int text_i = 0;
        int text_s = dirtyText.Length;

        /* does not allow regular spaces, tabs or any returns at the beginning of the cleanText */
        while (text_i != text_s && IsWhitespace(dirtyText[text_i])) {
            text_i++;
        }

        /* changes tabs to spaces, but does not allow returns inside the cleanText */
        while (text_i != text_s) {
            if (dirtyText[text_i] == '\n' || dirtyText[text_i] == '\r') {
                cleanText += ' ';
            }
            else if (dirtyText[text_i] != '\t') {
                cleanText += dirtyText[text_i];
            }
            text_i++;
        }

        /* changes multiple adjacent spaces into a single space */
        string cleanSingleSpaceText = "";
        int cleanText_s = cleanText.Length;
        text_i = 0;
        while (text_i != cleanText_s) {
            if (cleanText[text_i] == ' ') {
                cleanSingleSpaceText += cleanText[text_i];
                text_i++;
                while (text_i < cleanText_s && cleanText[text_i] == ' ') {
                    text_i++;
                }
            }
            else {
                cleanSingleSpaceText += cleanText[text_i];
                text_i++;
            }
        }
        return cleanSingleSpaceText;
    }

    public static string DisplayTextInterpret (string text) {
        string displayText = "";
        int text_i = 0;
        int text_s = text.Length;

        while (text_i != text_s) {
            if (text[text_i] == '\\') { // @cs
                text_i++;
                if (text_i == text_s) {
                    break;
                }   
                string specialChar = "";
                switch (text[text_i]) { /* special characters are preceded with a '\' character: '\A'  .  '&' */
                    case 'A': specialChar = "&";       break;
                    case 'B': specialChar = "\\";      break;
                    case 'C': specialChar = "%";       break;
                    case 'D': specialChar = "$";       break;
                    case 'H': specialChar = "#";       break;
                    case 'J': specialChar = "\u2060";  break; /* word-joiner */
                    case 'M': specialChar = "@";       break;
                    case 'N': specialChar = "\u00A0";  break; /* non-breaking space */
                    case 'P': specialChar = "|";       break;
                    case 'S': specialChar = " ";       break;
                    case 'T': specialChar = "~";       break;
                    case '1': specialChar = "[";       break;
                    case '2': specialChar = "]";       break;
                    case '3': specialChar = "{";       break;
                    case '4': specialChar = "}";       break;
                    default: Error("Incorrect special character declaration."); break;
                }
                if (specialChar != "") {
                    displayText += specialChar;
                }
            }
            else {
                displayText += text[text_i];
            }
            text_i++;
        }
        /* @TODO text formatting? *text* would be cursive, _text_ would be bold */
        return displayText;
    }

    public static string WrapText (string unwrapped, int width) {
        string wrapped = "";
        int currentColumn_i = 0;
        string currentWord = "";
        int unwrapped_s = unwrapped.Length;
        if (width != 0) {
            for (int i = 0; i < unwrapped_s; i++) {
                currentWord += unwrapped[i];
                if (unwrapped[i] == ' ' && currentWord.Length > 3) { /* if a word has more than two characters and comes across a space */
                    wrapped += currentWord;
                    currentWord = "";
                }
                if (currentWord.Length == width) { /* break it with at least 4 characters in new line */
                    if (width >= 10) {
                        wrapped += currentWord.Substring(0, width - 4);
                        char lastBreakChar = wrapped[wrapped.Length - 1];
                        if (lastBreakChar != ' ' && lastBreakChar != '-') { /* @TODO non-breaking space character too? */
                            wrapped += '-';
                        }
                        wrapped += '\n';
                        currentWord = currentWord.Substring(width - 4, 4);
                        currentColumn_i = 4;
                    }
                    else {
                        wrapped += currentWord + '\n';
                        currentWord = "";
                        currentColumn_i = 0;
                    }
                }

                if (currentColumn_i == width) {
                    wrapped += '\n';
                    currentColumn_i = currentWord.Length;
                }
                currentColumn_i++;
            }
            if (currentWord != "") {
                wrapped += currentWord;
            }
        }
        else {
            wrapped = unwrapped;
        }
        
        return wrapped;
    }

    public static bool IsTextVisible (string text) { /* determines whether the text has any non-whitespace character */
        int text_i = 0; int text_s = text.Length;
        while (text_i != text_s && IsWhitespace(text[text_i])) {
            text_i++;
        }
        return (text_i != text_s);
    }


// // // SCAN, CHECK, SEEK FUNCTIONS // // //


    public static void SeekUntil (string txt, ref int t_i, string endingChars) { /* increments the text index until it comes across one of the characters inside the endingChars argument */
        int chars_s = endingChars.Length; int i = 0;
        while (true) {
            for (i = 0; i < chars_s; i++) {
                if (txt[t_i] == endingChars[i]) { return; }
            }
            t_i++;
        }
    }

    public static void SeekEndOfConditional (string txt, ref int t_i) {
        /* &...&*.......||^..   * - starts here  ,  ^ - finishes there */
        int nestingDepth_b = 0;
        while (true) {
            SeekUntil(txt, ref t_i, "&|");
            if (txt[t_i] == '&') { /* &...& */
                nestingDepth_b++;
                SeekEndOfStatement(txt, ref t_i, "&");
            }
            else if (txt[t_i] == '|' && txt[t_i + 1] == '~') { /* |~ */
                Error("A conditional doesn't have its corresponding '||' symbol.", txt, t_i);
                break;
            }
            else if (txt[t_i] == '|' && txt[t_i + 1] == '|') { /* || */
                t_i += 2;
                if (nestingDepth_b != 0) { nestingDepth_b--; }
                else { break; } /* it finds the corresponding conditional ending here */
            }
            else if (txt[t_i] == '|') { /* | */
                t_i++;
            }
        }
    }

    public static void SeekEndOfChoiceRange (string txt, ref int t_i) {
        /* seeks end of 'current' choice range we are in */
        /* ...{...*...{..}..{..{}..}..{..}..}.... */
        int nestingDepth_b = 0;
        int pos_i = t_i;
        while (true) {
            SeekUntil(txt, ref t_i, "{}|");
            if (txt[t_i] == '{') {
                t_i++;
                SeekUntil(txt, ref t_i, "{}");

                if (txt[t_i] == '{') { /* {...{...}.... */
                    nestingDepth_b++;
                    SeekEndOfStatement(txt, ref t_i, "}"); /* {...{...}*... */
                }
                else if (txt[t_i] == '}') { /* {...}..... */
                    t_i++;
                    pos_i = t_i;
                }
            }
            else if (txt[t_i] == '}') { /* ....}.... */
                t_i++;
                if (nestingDepth_b != 0) { nestingDepth_b--; }
                else { break; }
            }
            else if (txt[t_i] == '|' && txt[t_i + 1] == '~') {
                t_i = pos_i;
                Error("The choice isn't in any choice range.", txt, t_i);
                break;
            }
            else if (txt[t_i] == '|') {
                t_i++;
            }
        }
        /* ...{.......{..}..{..{}..}..{..}..}*... */
    }

    public static void SeekEndOfStatement (string txt, ref int t_i, string endingChar) {
        t_i++; /* ?*....?.... */
        SeekUntil(txt, ref t_i, endingChar);
        t_i++; /* ?.....?*... */
    }


    public static string ScanTextUntil (string txt, ref int t_i, string endingChars) { /* return text until certain characters, modifies the t_i index! */
        /* ! - beginning char   ,   ? - endingChar   ,   * - caret position   ,   . - text we want to store */
        t_i++; /* !*....? */
        string storedText = "";
        int chars_s = endingChars.Length; int i = 0;
        while (true) {
            for (i = 0; i < chars_s; i++) {
                if (txt[t_i] == endingChars[i]) {
                    t_i++; /* !.....?* */
                    return RemoveWhitespace(storedText);
                }
            }
            storedText += txt[t_i];
            t_i++;
        }
    }


    /* checks for following scenario and returns true if so (can be any number of conditionals "&...&" before the "{...}" sequence): */
    /* &...&&...&&...&{...}..... */
    public static bool IsItConditionalChoice (string txt, int t_i) {
        /* * - caret position   ,   . - some text */
        /* &...&*...&&...&..... */       /* text index is at the caret position here, it is behind a '&' symbol */
        while (IsWhitespace(txt[t_i])) {
            t_i++;
        }
        if (txt[t_i] == '&') {
            while (true) {
                SeekEndOfStatement(txt, ref t_i, "&");
                while (IsWhitespace(txt[t_i])) {
                    t_i++;
                }
                if (txt[t_i] != '&') {
                    break;
                }
            }
        }
        /* &...&&...&&...&*.... */
        if (txt[t_i] == '{') {
            t_i++;
            SeekUntil(txt, ref t_i, "{}");
            if (txt[t_i] == '}') {
                return true;
            }
        }
        return false;
    }


// // // INSTRUCTION INTERPRETERS // // //


    public enum Operator { /* operator, its assigned number is the precedence */
        OP_NONE,
        OP_OR,OP_AND,
        OP_EQ,OP_NEQ,OP_GT,OP_GE,OP_LT,OP_LE, /* for condInterpreter */
        OP_ADD,OP_SUB,OP_MUL,OP_DIV,OP_MOD,OP_POW,  /* for both */
        OP_NOT,OP_LEN,OP_STR,OP_MIN,OP_MAX,OP_SUBSTR,
        OP_COMMA,OP_LP,OP_NLP,OP_RP,
        OP_IS,OP_EADD,OP_ESUB,OP_EMUL,OP_EDIV,OP_EMOD,OP_EPOW  /* for varInterpreter */
    };

    public static Operator StringToOperator (string opText) {
        /* for condInterpreter */
        if (opText == "OR")  { return Operator.OP_OR;  }
        if (opText == "AND") { return Operator.OP_AND; }
        if (opText == "(")   { return Operator.OP_LP;  }
        if (opText == "!(")  { return Operator.OP_NLP; }
        if (opText == ")")   { return Operator.OP_RP;  }
        if (opText == "==")  { return Operator.OP_EQ;  }
        if (opText == "!=")  { return Operator.OP_NEQ; }
        if (opText == ">")   { return Operator.OP_GT;  }
        if (opText == ">=")  { return Operator.OP_GE;  }
        if (opText == "<")   { return Operator.OP_LT;  }
        if (opText == "<=")  { return Operator.OP_LE;  }
        /* for varInterpreter */
        if (opText == "=")   { return Operator.OP_IS;   }
        if (opText == "+=")  { return Operator.OP_EADD; }
        if (opText == "-=")  { return Operator.OP_ESUB; }
        if (opText == "*=")  { return Operator.OP_EMUL; }
        if (opText == "/=")  { return Operator.OP_EDIV; }
        if (opText == "%=")  { return Operator.OP_EMOD; }
        if (opText == "^=")  { return Operator.OP_EPOW; }
        /* for both */
        if (opText == "+")   { return Operator.OP_ADD;   }
        if (opText == "-")   { return Operator.OP_SUB;   }
        if (opText == "*")   { return Operator.OP_MUL;   }
        if (opText == "/")   { return Operator.OP_DIV;   }
        if (opText == "%")   { return Operator.OP_MOD;   }
        if (opText == "^")   { return Operator.OP_POW;   }
        if (opText == ",")   { return Operator.OP_COMMA; }
        if (opText == "NOT") { return Operator.OP_NOT;   }
        if (opText == "LEN") { return Operator.OP_LEN;   }
        if (opText == "STR") { return Operator.OP_STR;   }
        if (opText == "MIN") { return Operator.OP_MIN;   }
        if (opText == "MAX") { return Operator.OP_MAX;   }
        if (opText == "SUBSTR") { return Operator.OP_SUBSTR;   }
        
        return Operator.OP_NONE;
    }
    
    public static int OperatorPrecedence (Operator op) {
        switch (op) {
            case Operator.OP_OR:                               return 1;
            case Operator.OP_AND:                              return 2;
            case Operator.OP_EQ:  case Operator.OP_NEQ: case Operator.OP_GT: 
            case Operator.OP_GE:  case Operator.OP_LT:  case Operator.OP_LE:     return 3;
            case Operator.OP_ADD: case Operator.OP_SUB:                 return 4;
            case Operator.OP_MUL: case Operator.OP_DIV: case Operator.OP_MOD:    return 5;
            case Operator.OP_POW:                              return 6;
            case Operator.OP_NOT: case Operator.OP_LEN: case Operator.OP_STR:
            case Operator.OP_MIN: case Operator.OP_MAX: 
            case Operator.OP_SUBSTR:                           return 7;
            default:                                  return 0;
        }
    }
    
    public static Pair Operation (State state, Pair elementA, Operator op, Pair elementB) {
        int number = 0;
        switch (op) {
            case Operator.OP_EQ:  number = (elementA.Item1 == elementB.Item1) ? 1 : 0; break;
            case Operator.OP_NEQ: number = (elementA.Item1 != elementB.Item1) ? 1 : 0; break;
            case Operator.OP_AND: number = (elementA.Item1 != 0 && elementB.Item1 != 0) ? 1 : 0; break;
            case Operator.OP_OR:  number = (elementA.Item1 != 0 || elementB.Item1 != 0) ? 1 : 0; break;
            case Operator.OP_GT:  number = (elementA.Item1 >  elementB.Item1) ? 1 : 0; break;
            case Operator.OP_GE:  number = (elementA.Item1 >= elementB.Item1) ? 1 : 0; break;
            case Operator.OP_LT:  number = (elementA.Item1 <  elementB.Item1) ? 1 : 0; break;
            case Operator.OP_LE:  number = (elementA.Item1 <= elementB.Item1) ? 1 : 0; break;
            case Operator.OP_ADD: number = (elementA.Item1 + elementB.Item1); break;
            case Operator.OP_SUB: number = (elementA.Item1 - elementB.Item1); break;
            case Operator.OP_MUL: number = (elementA.Item1 * elementB.Item1); break;
            case Operator.OP_DIV: if (elementB.Item1 != 0) { number = (elementA.Item1 / elementB.Item1); } else { /* @TODO error divided by zero*/ } break;
            case Operator.OP_MOD: number = (elementA.Item1 % elementB.Item1); break;
            case Operator.OP_POW: number = (int)Math.Pow(elementA.Item1, elementB.Item1); break;
            case Operator.OP_MIN: number = Math.Min(elementA.Item1, elementB.Item1); break;
            case Operator.OP_MAX: number = Math.Max(elementA.Item1, elementB.Item1); break;
            default: {
                Error("Wrong operator inside the integer instruction.", state.text, state.currentPos.text_i);
                if (state.currentPos.condNestingDepth < state.condElse.Count) {
                    state.condElse[state.currentPos.condNestingDepth] = true;
                }
                return new Pair(number, "");
            }
        }
        return new Pair(number, "");
    }
    
    public static bool IsFunctionOperator (Operator op) {
        if (op == Operator.OP_NOT || op == Operator.OP_LEN || op == Operator.OP_STR || op == Operator.OP_MIN || op == Operator.OP_MAX || op == Operator.OP_SUBSTR) { return true; }
        return false;
    }
    
    public static bool IsRightAssociativeOperator (Operator op) {
        if (IsFunctionOperator(op) || op == Operator.OP_POW || op == Operator.OP_AND || op == Operator.OP_OR) { return true; }
        return false;
    }
    
    public static bool IsSingleArgumentOperator (Operator op) {
        if (op == Operator.OP_NOT || op == Operator.OP_LEN || op == Operator.OP_STR) { return true; }
        return false;
    }
    
    public static bool IsTripleArgumentOperator (Operator op) {
        if (op == Operator.OP_SUBSTR) { return true; }
        return false;
    }

    public static List<string> SplitInstrSegments (string instrText) { /* splits all instruction segments: "!(Var1 == Var2)"  .  "!(" "Var1" "==" "Var2" ")" */
        List<string> segments = new List<string>();
        int i = 0;
        int instrText_s = instrText.Length;
        while (i + 1 < instrText.Length) {
            if ((instrText[i] == '(' && instrText[i + 1] != ' ') || 
                (instrText[i] != ' ' && instrText[i] != '!' && instrText[i + 1] == '(') || 
                (i + 2 < instrText.Length && instrText[i] != ' ' && instrText[i + 1] == '!' && instrText[i + 2] == '(') || 
                (instrText[i] != ' ' && instrText[i + 1] == ')') || 
                (instrText[i] == ')' && instrText[i + 1] != ' ') || 
                (instrText[i] != ' ' && instrText[i + 1] == ',') || 
                (instrText[i] == ',' && instrText[i + 1] != ' ')) {
                instrText = instrText.Insert(i + 1, " ");
            }
            i++;
        }
        if (instrText.Length != 0 && instrText[instrText.Length - 1] == ' ') {
            instrText.Substring(0,(instrText.Length - 1)); /* remove a space at the end (if there's one) */
        }
        i = 0; instrText_s = instrText.Length; 
        string segmentText_b = ""; 
        while (true) {
            while (i != instrText_s && instrText[i] != ' ' && instrText[i] != '"') { /* @TODO add the (') sign here too */
                segmentText_b += instrText[i];
                i++;
            }
            if (i != instrText_s && instrText[i] == '"') {
                segmentText_b += instrText[i];
                i++;
                while (i != instrText_s && instrText[i] != '"') { /* @TODO add the (') sign here too */
                    segmentText_b += instrText[i];
                    i++;
                }
                if (i != instrText_s) {
                    segmentText_b += instrText[i];
                    i++;
                    if (i != instrText_s && instrText[i] != ' ') {
                        continue;
                    }
                }
            }
            segments.Add(segmentText_b);
            segmentText_b = "";
            if (i == instrText_s) { break; }
            i++;
        }
        return segments;
    }

    public static Pair GetValue (State state, string varText) { /* checks whether the string is a variable or a plain number, and then gets the integer value of said number or the value behind the variable */
        bool isNegated = false;
        if (varText.Length != 0 && varText[0] == '!') {
            isNegated = true;
            varText = varText.Substring(1);
        }
        
        bool hasSucceeded = false;
        int converted = stringToInt(varText, ref hasSucceeded);
        if (hasSucceeded) { /* conversion success, it is a number */
            if (isNegated) {
                return new Pair((converted != 0) ? 0 : 1, "");                
            }
            else {
                return new Pair(converted, "");                
            }
        }
        else {             /* conversion fail, it is a variable */
            int varText_s = varText.Length;
            if (varText_s >= 2 && varText[0] == '"' && varText[varText_s - 1] == '"') { /* @TODO add (') characters alongside (") too */
                string stringVal = varText.Substring(1, varText_s - 2);
                return new Pair(0, stringVal);
            }
            return GetVar(state, varText, isNegated);
        }
    }
    
    public static Pair OperationsInterpret (State state, List<string> segments) {
        int segments_s = segments.Count;
        int segment_i = 0;
        
        List<string> output = new List<string>();
        List<string> operators = new List<string>();;
        while (segment_i != segments_s) {
            Operator op = StringToOperator(segments[segment_i]);
            if (op == Operator.OP_NONE) { /* is a value */
                output.Add(segments[segment_i]);
            }
            else if (IsFunctionOperator(op)) {
                operators.Add(segments[segment_i]);
            }
            else if (op == Operator.OP_COMMA) {
                while (operators.Count != 0 && StringToOperator(operators[operators.Count - 1]) != Operator.OP_LP) {
                    output.Add(operators[operators.Count - 1]);
                    operators.RemoveAt(operators.Count - 1);
                }
                if (operators.Count == 0) {
                    Error("Lone comma outside of function parentheses.", state.text, state.currentPos.text_i);
                    return new Pair(0, "");
                }
            }
            else if (op == Operator.OP_NLP) {
                operators.Add("NOT");
                operators.Add("(");
            }
            else if (op == Operator.OP_LP) {
                operators.Add(segments[segment_i]);
            }
            else if (op == Operator.OP_RP) {
                while (operators.Count != 0 && StringToOperator(operators[operators.Count - 1]) != Operator.OP_LP) {
                    output.Add(operators[operators.Count - 1]);
                    operators.RemoveAt(operators.Count - 1);
                }
                if (operators.Count != 0) { // @TODO ? should be without ! ??
                    operators.RemoveAt(operators.Count - 1);
                    if (operators.Count != 0 && IsFunctionOperator(StringToOperator(operators[operators.Count - 1]))) {
                        output.Add(operators[operators.Count - 1]);
                        operators.RemoveAt(operators.Count - 1);
                    }
                }
                else {
                    Error("Mismatched parentheses.", state.text, state.currentPos.text_i);
                    return new Pair(0, "");
                }
            }
            else { /* is a non-parenthesis operator */
                Operator op2 = Operator.OP_NONE;
                if (operators.Count != 0) {
                    op2 = StringToOperator(operators[operators.Count - 1]);
                }
                while (operators.Count != 0 && op2 != Operator.OP_LP && ( OperatorPrecedence(op2) > OperatorPrecedence(op) || ( OperatorPrecedence(op2) == OperatorPrecedence(op) && !IsRightAssociativeOperator(op) ) )) {
                    output.Add(operators[operators.Count - 1]);
                    operators.RemoveAt(operators.Count - 1);
                    if (operators.Count != 0) {
                        op2 = StringToOperator(operators[operators.Count - 1]);
                    }
                    else {
                        break;
                    }
                }
                operators.Add(segments[segment_i]);
            }
            
            segment_i++;
        }

        while (operators.Count != 0) {
            Operator op = StringToOperator(operators[operators.Count - 1]);
            if (op == Operator.OP_LP || op == Operator.OP_RP) {
                Error("Mismatched parentheses.", state.text, state.currentPos.text_i);
                return new Pair(0, "");
            }
            output.Add(operators[operators.Count - 1]);
            operators.RemoveAt(operators.Count - 1);
        }
        
        
        int output_i = 0;
        int output_s = output.Count;
        List<string>                 stackSegments = new List<string>();
        List<Pair> stackValues = new List<Pair>();
        Pair elementA, elementB, elementC;
        while (output_i != output_s) {
            Operator op = StringToOperator(output[output_i]);
            if (op == Operator.OP_NONE) {
                stackSegments.Add(output[output_i]); stackValues.Add(GetValue(state, output[output_i]));
            }
            else {
                if (IsSingleArgumentOperator(op)) {
                    if (stackValues.Count == 0) { goto OperationError; } elementA = stackValues[stackValues.Count - 1]; stackValues.RemoveAt(stackValues.Count - 1); stackSegments.RemoveAt(stackSegments.Count - 1);
                    switch (op) {
                        case Operator.OP_NOT: {
                            stackSegments.Add("0"); stackValues.Add(new Pair((elementA.Item1 != 0) ? 0 : 1, ""));
                            break;
                        }
                        case Operator.OP_LEN: {
                            stackSegments.Add("0"); stackValues.Add(new Pair((elementA.Item2).Length, ""));
                            break;
                        }
                        case Operator.OP_STR: {
                            stackSegments.Add("0"); stackValues.Add(new Pair(0, (elementA.Item1).ToString()));
                            break;
                        }
                        default: break;
                    }
                }
                else if (IsTripleArgumentOperator(op)) {
                    if (stackValues.Count == 0) { goto OperationError; } elementA = stackValues[stackValues.Count - 1]; stackValues.RemoveAt(stackValues.Count - 1); stackSegments.RemoveAt(stackSegments.Count - 1);
                    if (stackValues.Count == 0) { goto OperationError; } elementB = stackValues[stackValues.Count - 1]; stackValues.RemoveAt(stackValues.Count - 1); stackSegments.RemoveAt(stackSegments.Count - 1);
                    if (stackValues.Count == 0) { goto OperationError; } elementC = stackValues[stackValues.Count - 1]; stackValues.RemoveAt(stackValues.Count - 1); stackSegments.RemoveAt(stackSegments.Count - 1);
                    switch (op) {
                        case Operator.OP_SUBSTR: {
                            if (elementB.Item1 >= 0 && elementB.Item1 <= (int)(elementC.Item2).Length) {
                                stackSegments.Add("0"); stackValues.Add(new Pair(0, (elementC.Item2).Substring(elementB.Item1, elementA.Item1)));
                            }
                            else {
                                Error("Second argument in a substring operation is invalid.", state.text, state.currentPos.text_i);
                                state.condElse[state.currentPos.condNestingDepth] = true;
                                return new Pair(0, "");
                            }
                            break;
                        }
                        default: break;
                    }
                }
                else {
                    bool isString = false;
                    if (stackValues.Count == 0) { goto OperationError; } elementA = stackValues[stackValues.Count - 1]; stackValues.RemoveAt(stackValues.Count - 1);
                    if (stackValues.Count == 0) { goto OperationError; } elementB = stackValues[stackValues.Count - 1]; stackValues.RemoveAt(stackValues.Count - 1);
                    if (elementA.Item2 != "" || elementB.Item2 != "") {
                        isString = true;
                    }
                    if (stackSegments[stackSegments.Count - 1] == "\"\"") { isString = true; } stackSegments.RemoveAt(stackSegments.Count - 1);
                    if (stackSegments[stackSegments.Count - 1] == "\"\"") { isString = true; } stackSegments.RemoveAt(stackSegments.Count - 1);
                    
                    if (isString) { /* @TODO include (') characters for quotation */
                        switch (op) {
                            case Operator.OP_ADD: stackSegments.Add("\"" + elementB.Item2 + elementA.Item2 + "\""); stackValues.Add(new Pair(0, elementB.Item2 + elementA.Item2)); break;
                            case Operator.OP_EQ:  stackSegments.Add("0"); stackValues.Add(new Pair((elementB.Item2 == elementA.Item2) ? 1 : 0, "")); break;
                            case Operator.OP_NEQ: stackSegments.Add("0"); stackValues.Add(new Pair((elementB.Item2 != elementA.Item2) ? 1 : 0, "")); break;
                            default: {
                                Error("Wrong operator inside the string instruction.", state.text, state.currentPos.text_i);
                                state.condElse[state.currentPos.condNestingDepth] = true;
                                return new Pair(0, "");
                            }
                        }
                    }
                    else {
                        // @TODO check for errors
                        if (IsRightAssociativeOperator(op)) {
                            stackSegments.Add("0"); stackValues.Add(Operation(state, elementA, op, elementB));
                        }
                        else {
                            stackSegments.Add("0"); stackValues.Add(Operation(state, elementB, op, elementA));
                        }
                    }
                }
            }    
            output_i++;
        }
        
        if (stackValues.Count == 0) {
            return new Pair(0, "");
        }
        else {
            return stackValues[stackValues.Count - 1];      
        }
        
        OperationError:
        
        Error("Invalid operation.", state.text, state.currentPos.text_i);
        state.condElse[state.currentPos.condNestingDepth] = true;
        return new Pair(0, "");
    }


    public static void VarInstrInterpret (State state, string instrText) { /* variable instructions interpreter */
        List<string> segments = SplitInstrSegments(instrText);
        if (segments.Count == 1) { /* shortcuts used for setting variable to true/false or incrementing/decrementing */
            int segment_s = segments[0].Length;
            if (segment_s >= 2 && segments[0][segment_s - 1] == '+' && segments[0][segment_s - 2] == '+') {
                GetVar(state, segments[0].Substring(0, segment_s - 2), false).Item1++; /* increment if ends with the '++' characters */
            }
            else if (segment_s >= 2 && segments[0][segment_s - 1] == '-' && segments[0][segment_s - 2] == '-') {
                GetVar(state, segments[0].Substring(0, segment_s - 2), false).Item1--; /* decrement if ends with the '--' characters */
            }
            else if (segment_s >= 1 && segments[0][0] == '!') {
                GetVar(state, segments[0].Substring(1), false).Item1 = 0;            /* '!' at beginning sets to false; erase() gets rid of '!' character */
            }
            else {
                GetVar(state, segments[0], false).Item1 = 1;                        /* otherwise sets to true */
            }
        }
        else if (segments.Count >= 3) {
            string lVal = segments[0];
            Operator op = StringToOperator(segments[1]);
            if (op == Operator.OP_NONE) {
                Error("No left-hand side value in a variable instruction.", state.text, state.currentPos.text_i);
                return;
            }
            segments.RemoveAt(0); segments.RemoveAt(0);
            Pair rVal = OperationsInterpret(state, segments);
            if (rVal.Item2 != "") { /* @TODO include (') also alongside (")  */
                switch (op) {
                    case Operator.OP_IS:   GetVar(state, lVal, false).Item2 =  rVal.Item2; break;
                    case Operator.OP_EADD: GetVar(state, lVal, false).Item2 += rVal.Item2; break;
                    default: Error("Wrong operator inside the string variable instruction.", state.text, state.currentPos.text_i); break;
                }
            }   
            else {
                GetVar(state, lVal, false).Item2 = "";
                switch (op) {
                    case Operator.OP_IS:   GetVar(state, lVal, false).Item1 =  rVal.Item1; break;
                    case Operator.OP_EADD: GetVar(state, lVal, false).Item1 += rVal.Item1; break;
                    case Operator.OP_ESUB: GetVar(state, lVal, false).Item1 -= rVal.Item1; break;
                    case Operator.OP_EMUL: GetVar(state, lVal, false).Item1 *= rVal.Item1; break;
                    case Operator.OP_EDIV: GetVar(state, lVal, false).Item1 /= rVal.Item1; break;
                    case Operator.OP_EMOD: GetVar(state, lVal, false).Item1 %= rVal.Item1; break;
                    default: Error("Wrong operator inside the integer variable instruction.", state.text, state.currentPos.text_i); break;
                }
            }
        }
    }

    public static void SpecInstrInterpret (State state, string instrText) { /* special instructions interpreter */
        List<string> segments = SplitInstrSegments(instrText);
        int segments_s = segments.Count;

        bool wasCommandFound = false;
        bool hasEnoughArguments = true;
        int textLength = segments[0].Length;
        while (textLength != 0 && !wasCommandFound) {
            string textSegment = segments[0].Substring(0, textLength);


            /* displays number in the text itself: "#Money = 50#I have @DISPLAY Money@ dollars."  .  "I have 50 dollars." */
            if (textSegment == "DISPLAY".Substring(0, textLength)) {
                if (segments_s < 2) { hasEnoughArguments = false; break; }
                segments.RemoveAt(0);
                Pair varText = OperationsInterpret(state, segments);
                if (varText.Item2 != "") {
                    state.displayText += varText.Item2;
                }
                else {
                    state.displayText += (varText.Item1).ToString();
                }
                wasCommandFound = true;
            }
            /* saves the game to a file */
            else if (textSegment == "SAVE".Substring(0, textLength)) {
                // StateSave(state); /* @TODO */
                wasCommandFound = true;
            }
            /* resets the values of 'temporary' variables (those starting with lowercase) */
            else if (textSegment == "RESET".Substring(0, textLength)) {
                /*ResetVars();*/
                wasCommandFound = true;
            }
            else if (textSegment == "WAIT".Substring(0, textLength)) {
                if (segments_s < 2) { hasEnoughArguments = false; break; }
                string waitNumberText = segments[1];
                bool hasSucceeded = false; int waitNumber = (int)stringToInt(waitNumberText, ref hasSucceeded);
                if (hasSucceeded) {
                    //waitNumber = waitNumber; /* @TODO remove later */
                }
                else {
                    Error("Following number could not be interpreted inside the special instruction: " + waitNumberText, state.text, state.currentPos.text_i);
                }
                wasCommandFound = true;
            }
            else if (textSegment == "IMG".Substring(0, textLength)) {
                if (segments_s < 2) { hasEnoughArguments = false; break; }
                string imgName = segments[1];
                state.frames.Add(imgName);
                state.shouldChangeFrame = true;
                wasCommandFound = true;
            }
            textLength--;
        }
        if (!wasCommandFound)    { Error("Unspecified command inside the special instruction.", state.text, state.currentPos.text_i); }
        if (!hasEnoughArguments) { Error("Not enough arguments inside the special instruction.", state.text, state.currentPos.text_i); }
    }

    public static void PersCondInstrInterpret (State state, string instrText) { /* $[5] Count < 5$ */
        if (instrText == "") { Error("Persistent conditional instruction is empty.", state.text, state.currentPos.text_i); }
        bool shouldDeactivate = false;
        if (instrText.Length != 0 && instrText[0] == '~') {
            instrText = instrText.Substring(1); /* removes the '~' character used for the skip */
            shouldDeactivate = true;
        }
        int instrText_s = instrText.Length;
        int instrText_i = 0;

        string jumpPointInstrText = "";

        while (instrText_i != instrText_s && instrText[instrText_i] != '[') { instrText_i++; }
        if (instrText_i == instrText_s) { return; }
        instrText_i++;
        while (instrText_i != instrText_s && instrText[instrText_i] != ']') { jumpPointInstrText += instrText[instrText_i]; instrText_i++; }
        if (instrText_i == instrText_s) { return; }
        instrText_i++;

        while (instrText_i != instrText_s && instrText[instrText_i] == ' ') { instrText_i++; }
        if (instrText_s == instrText_i) { return; }
        
        if (instrText_i >= instrText.Length) { Error("Persistent conditional instruction is too short.", state.text, state.currentPos.text_i); }
        string condInstrText = instrText.Substring(instrText_i);
        if (shouldDeactivate) { // removes/deactivates the persistent conditional
            int persCond_s = state.persCond.Count;
            for (int i = 0; i < persCond_s; i++) {
                if (state.persCond[i].Item1 == jumpPointInstrText && state.persCond[i].Item2 == condInstrText) {
                    state.persCond.RemoveAt(i);
                    break;
                }
            }
        }
        else { // activates the persistent conditional
            state.persCond.Add((jumpPointInstrText, condInstrText));
        }
    }
    
    public static void JumpPointInstrInterpret (State state, string instrText) {
        int instrText_s = instrText.Length;
        if (instrText_s != 0 && instrText[0] == '~') {
            int jumpHistory_s = state.jumpHistory.Count;
            
            if (instrText_s == 1) {
                if (jumpHistory_s != 0) {
                    state.currentPos.text_i = (state.jumpHistory[jumpHistory_s - 1]).Item2.text_i;
                    state.currentPos.condNestingDepth = (state.jumpHistory[jumpHistory_s - 1]).Item2.condNestingDepth;
                    state.condElse.Clear(); /* resets the ELSE statement on all depths */
                }
            }
            else {
                instrText = instrText.Substring(1);
            
                bool hasSucceeded = false; int jumpPointNumber_i = stringToInt(instrText, ref hasSucceeded);
                if (hasSucceeded) {
                    if (state.jumpBasePos.ContainsKey(jumpPointNumber_i)) { /* checks if there's a jump base with such number */
                        for (int i = jumpHistory_s - 1; i >= 0; i--) {
                            if ((state.jumpHistory[i]).Item1 == jumpPointNumber_i) {
                                state.currentPos.text_i = (state.jumpHistory[i]).Item2.text_i;
                                state.currentPos.condNestingDepth = (state.jumpHistory[i]).Item2.condNestingDepth;
                                state.condElse.Clear(); /* resets the ELSE statement on all depths */
                                break;
                            }
                        }
                    }
                    else {
                        Error("Couldn't perform the jump, there's no jump base with the following number: " + instrText, state.text, state.currentPos.text_i);
                    }
                }
                else {
                    Error("Following jump point number could not be interpreted: " + instrText, state.text, state.currentPos.text_i);
                }
            }
        }
        else {
            bool hasSucceeded = false; int jumpPointNumber_i = stringToInt(instrText, ref hasSucceeded);
            if (hasSucceeded) {
                if (state.jumpBasePos.ContainsKey(jumpPointNumber_i)) { /* checks if there's a jump base with such number */
                    state.jumpHistory.Add((jumpPointNumber_i, new Pos(state.currentPos.text_i, state.currentPos.condNestingDepth)));
                    state.currentPos.text_i = state.jumpBasePos[jumpPointNumber_i].text_i;
                    state.currentPos.condNestingDepth = state.jumpBasePos[jumpPointNumber_i].condNestingDepth;
                    state.condElse.Clear(); /* resets the ELSE statement on all depths */
                }
                else {
                    Error("Couldn't perform the jump, there's no jump base with the following number: " + instrText, state.text, state.currentPos.text_i);
                }
            }
            else {
                Error("Following jump point number could not be interpreted: " + instrText, state.text, state.currentPos.text_i);
            }
        }
    }

    public static bool CondInstrInterpret (State state, string instrText) {  /* conditional instructions interpreter, returns if the condition is true or false */
        if (instrText.Length != 0 && instrText[0] == '~') {
            instrText = instrText.Substring(1); /* removes the '~' character used for the skip */
        }

        List<string> segments = SplitInstrSegments(instrText);

        while (state.currentPos.condNestingDepth >= state.condElse.Count) {
            state.condElse.Add(false);
        }
        if (segments[0] == "ELSE") {
            if (state.condElse[state.currentPos.condNestingDepth] == false) {
                return false;
            }
            else {
                segments.RemoveAt(0); /* removes the 'ELSE' segment on the beginning */
            }
        }
        if (segments.Count == 0) {
            state.condElse[state.currentPos.condNestingDepth] = false;
            return true;
        }

        bool result = (OperationsInterpret(state, segments).Item1 != 0) ? true : false;
        state.condElse[state.currentPos.condNestingDepth] = !result; /* "ELSE" is always an opposite of the result */
        return result;
    }
    
    public static void AddTextObject (State state, string text, TextType type) {
        if (state == null) { return; }
        TextObject textObj = new TextObject();
        textObj.actor_n  = state.actor_n;
        textObj.text     = text;
        textObj.type     = type;
        textObj.yPos     = 0.0f;
        textObj.yDestPos = 0.0f;
        textObj.height   = 0.0f;
        
        state.textObjs.Add(textObj);
    }
    
    public static void ShowRefreshedText (State state) {
        if (state == null) { return; }
        //system("cls");
        string showText = "";
        for (int i = 0; i < state.textObjs.Count; i++) {
            //std::cout<<state.textObjs[i].text<<'\n';
            showText += state.textObjs[i].text + "\n";
            //Debug.Log(state.textObjs[i].text + "\n");
        }
        Debug.Log(showText);
    }

    public static string ChoiceNumberPrefix (int index) {
        //return "{" + std::to_string(index + 1) + "} ";
        return (index + 1).ToString() + ". ";
    }

    public static void RefreshAccentedChoices (State state) {
        if (state == null) { return; }
        int textObjs_s = state.textObjs.Count;
        int choices_s = state.choices.Count;
        for (int i = 0; i < choices_s; i++) {
            if (state.choices[i].type == TextType.CHOICE_ACCENTED) {
                if (state.choices[i].accentedOptions.ContainsKey(state.currentAccent.Current)) { /* does the choice has current accent */
                    state.choices[i].displayText = state.choices[i].accentedOptions[state.currentAccent.Current];
                    string choiceNumberedText = ChoiceNumberPrefix(i) + state.choices[i].displayText;
                    choiceNumberedText = DisplayTextInterpret(choiceNumberedText);
                    choiceNumberedText = WrapText(choiceNumberedText, state.textWidth);
                    state.textObjs[textObjs_s - choices_s + i].text = choiceNumberedText;
                }
                else { /* the accented option is unavailable for current accent */
                    string choiceNumberedText = ChoiceNumberPrefix(i) + "<Unavailable.>";
                    state.textObjs[textObjs_s - choices_s + i].text = choiceNumberedText;
                }
            }
        }
        ShowRefreshedText(state);
    }

    public static void ShowText (State state, string text) {
        if (state == null) { return; }
        text = RemoveWhitespace(text);
        text = DisplayTextInterpret(text);
        text = WrapText(text, state.textWidth);
        
        AddTextObject(state, text, TextType.NORMAL);
        //std::cout<<text<<std::endl;
        //Debug.Log(text + "\n");

        //#ifdef DIAL_DEBUG
        //isBacktrackLocked = false;
        //#endif
    }

    public static void ShowChoices (State state) {
        if (state == null) { return; }
        string choiceText = "";
        int choices_s = state.choices.Count;
        for (int i = 0; i < choices_s; i++) {
            choiceText = state.choices[i].displayText;
            choiceText = DisplayTextInterpret(choiceText);
            choiceText = WrapText(choiceText, state.textWidth);

            string numberedChoiceText = ChoiceNumberPrefix(i) + choiceText;

            AddTextObject(state, numberedChoiceText, state.choices[i].type);
            //std::cout<<numberedChoiceText<<'\n';
            //Debug.Log(numberedChoiceText + "\n");
        }
        RefreshAccentedChoices(state);
    }
    
    /* interprets the instrText and assigns values to the displayText, type and accentedOptions of state's choices */
    public static void ChoicesInterpret (State state) {
        int choices_s = state.choices.Count;
        for (int i = 0; i < choices_s; i++) {
            string choiceText = state.choices[i].instrText;
            int choiceText_s = choiceText.Length;
            if (choiceText_s != 0 && choiceText[0] == '~') {
                choiceText = choiceText.Substring(1);
                choiceText_s--;
            }
            
            string analysedChoiceText = "";
            /* scan for conditionals inside the choice */
            if (choiceText_s != 0) {
                State state_b = new State();
                state_b.text = choiceText + "|~ ";
                state_b.text_s = state_b.text.Length;
                
                state_b.currentPos = new Pos();
                state_b.currentPos.text_i = 0;
                state_b.currentPos.condNestingDepth = 0;
                string txt = state_b.text;
                ref int t_i = ref state_b.currentPos.text_i;
                while (t_i < choiceText_s) {
                    switch (txt[t_i]) {
                        case '&': { 
                            /* @TODO make REPEAT variable available, hard to make it work though without making the code dirty */
                            string instrText = ScanTextUntil(txt, ref t_i, "&");
                            if (IsItConditionalChoice(txt, t_i)) {
                                SeekEndOfChoiceRange(txt, ref t_i);
                            }
                            else {
                                bool isConditionTrue = CondInstrInterpret(state_b, instrText);
                                if (isConditionTrue) {
                                    state_b.currentPos.condNestingDepth++;
                                }
                                else {
                                    SeekEndOfConditional(txt, ref t_i);
                                }
                            }
                            break;
                        }
                        case '|': { 
                            if (txt[t_i + 1] == '|') { /* || */
                                t_i += 2;
                                state_b.currentPos.condNestingDepth--;
                            }
                            else { /* | */
                                analysedChoiceText += txt[t_i];
                                t_i++;
                            }
                            break;
                        }
                        default: { 
                            analysedChoiceText += txt[t_i];
                            t_i++;
                            break;
                        }
                    }
                }
            }
            
            choiceText = RemoveWhitespace(analysedChoiceText);
            TextType choiceType = TextType.CHOICE_NORMAL;
            
            if (choiceText[0] == '|') { /* accented choice */
                choiceType = TextType.CHOICE_ACCENTED;
                choiceText_s = choiceText.Length;
                int t_i = 1;
                string accentName = "";
                string accentedText = "";
                while (t_i != choiceText_s) {
                    while (t_i != choiceText_s && choiceText[t_i] != ' ' && choiceText[t_i] != ':') {
                        accentName += choiceText[t_i];
                        t_i++;
                    }
                    if (choiceText[t_i] == ' ' || choiceText[t_i] == ':') {
                        while (t_i != choiceText_s && (choiceText[t_i] == ' ' || choiceText[t_i] == ':')) {
                            t_i++;
                        }
                        while (t_i != choiceText_s && choiceText[t_i] != '|') {
                            accentedText += choiceText[t_i];
                            t_i++;
                        }
                        if (t_i != choiceText_s && choiceText[t_i] == '|') {
                            t_i++;
                        }
                        
                        if (Char.IsUpper(accentName, 0)) { 
                            Error("Accented choice option's first character must be lowercase as it has to be a local variable.", state.text, state.currentPos.text_i);
                            break;
                        }
                        else {
                            state.localVars[accentName] = new Pair(0, "");
                        }
                        state.choices[i].accentedOptions[accentName] = accentedText;
                        accentName = "";
                        accentedText = "";
                    }
                    else {
                        Error("The accented choice option doesn't have its corresponding text; it might be missing a space after the accent's name.", state.text, state.currentPos.text_i);
                        break;
                    }
                }
                choiceText = "";
            }
            /* @TODO else if '%' for chance? */

            state.choices[i].displayText = choiceText;
            state.choices[i].type = choiceType;
        }
        state.possibleAccents.Clear();
        bool hasAccentedChoice = false;
        for (int i = 0; i < choices_s; i++) {
            if (state.choices[i].type == TextType.CHOICE_ACCENTED) {
                hasAccentedChoice = true;
                foreach (var it in state.choices[i].accentedOptions) {
                    state.possibleAccents.Add(it.Key);
                }
            }
        }
        if (hasAccentedChoice) {
            state.currentAccent = state.possibleAccents.GetEnumerator();
            state.currentAccent.Reset();
            state.currentAccent.MoveNext();
        }
    }
    
// // // EXTERNAL FUNCTIONS // // //

    public static bool IsCurrentStatus (State state, Status status) {
        if (state == null) { return false; }
        return (state.status == status);
    }
    
    public static bool IsChoiceValid (State state, int choice_i) {
        if (state == null) { return false; }
        
        if (state.choices[choice_i].type == TextType.CHOICE_ACCENTED) {
            string currentAccentName = state.currentAccent.Current;
            if (state.choices[choice_i].accentedOptions.ContainsKey(currentAccentName) == false) {
                return false;
            }
        }
        return true;

    }
    public static void Choice (State state, int choice_i) {
        if (state == null) { return; }
        
        if (IsCurrentStatus(state, Status.WAIT_FOR_CHOICE)) {
            if (state.choices[choice_i].type == TextType.CHOICE_ACCENTED) {
                string currentAccentName = state.currentAccent.Current;
                if (state.choices[choice_i].accentedOptions.ContainsKey(currentAccentName)) {
                    state.localVars[currentAccentName] = new Pair(1, "");
                    state.saveData.Add("a:" + currentAccentName);
                }
                else { /* ignores the user's choice as this accented choice is unavailable */
                    return;
                }
            }
            
            int choicePos_i = state.choices[choice_i].jumpPos.text_i;
            state.hasOneUseChoiceRecurred[choicePos_i] = true; /* @TODO change the naming here, also delete this if statement */
             
            TextObject tempText = state.textObjs[state.textObjs.Count - (state.choices.Count - choice_i - 1) - 1];
            /* deletes from textObjs until it comes across a NORMAL or CHOICE_SELECTED type */
            while (state.textObjs.Count != 0 && state.textObjs[state.textObjs.Count - 1].type != TextType.NORMAL && state.textObjs[state.textObjs.Count - 1].type != TextType.CHOICE_SELECTED) { /* deletes until texttype == normal or size is 0 */
                state.textObjs.RemoveAt(state.textObjs.Count - 1);
            }
            /* then adds our selected choice to the textObjs pool */
            string displayText = state.choices[choice_i].displayText;
            displayText = DisplayTextInterpret(displayText);
            displayText = WrapText(displayText, state.textWidth);
            if (state.flags.HasFlag(Flags.LEAVE_PREFIX_AFTER_CHOICE)) {
                displayText = ChoiceNumberPrefix(choice_i) + displayText;
            }
            
            AddTextObject(state, displayText, TextType.CHOICE_SELECTED);
            state.textObjs[state.textObjs.Count - 1].yPos     = tempText.yPos;
            state.textObjs[state.textObjs.Count - 1].yDestPos = tempText.yDestPos;
            state.textObjs[state.textObjs.Count - 1].height   = tempText.height;
            
            ShowRefreshedText(state);

            state.currentPos = state.choices[choice_i].jumpPos;
            state.saveData.Add((choice_i + 1).ToString());
            state.status = Status.INTERPRET;
            Interpret(state);
        }
    }
    
    public static void Continuation (State state) {
        if (state == null) { return; }
        if (IsCurrentStatus(state, Status.WAIT_FOR_CONTINUATION)) {
            state.saveData.Add("0");
            state.status = Status.INTERPRET;
            Interpret(state);
        }
    }
    
    public static void AccentIncrement (State state) {
        if (state == null) { return; }
        
        if (state.possibleAccents.Count != 0) {
            if (!state.currentAccent.MoveNext()) { state.currentAccent.Reset(); state.currentAccent.MoveNext(); }
            //else { state.currentAccent.MoveNext(); }
            RefreshAccentedChoices(state);
        }
    }
    
    public static void AccentDecrement (State state) {
        if (state == null) { return; }
        
        if (state.possibleAccents.Count != 0) {
            //if (state.currentAccent == state.possibleAccents.begin()) { state.currentAccent = --state.possibleAccents.end(); }
            //else { state.currentAccent--; }
            RefreshAccentedChoices(state);
        }
    }
    
    public static bool HasOneUseChoiceRecurred (State state, int choice_i) {
        if (state == null) { return false; }
        if (!state.hasOneUseChoiceRecurred.ContainsKey(state.choices[choice_i].jumpPos.text_i)) { state.hasOneUseChoiceRecurred[state.choices[choice_i].jumpPos.text_i] = false; }
        return state.hasOneUseChoiceRecurred[state.choices[choice_i].jumpPos.text_i];
    }
    
    public static int GetChoicesSize (State state) {
        if (state == null) { return 0; }
        return state.choices.Count;
    }
    
    public static int GetTextsSize (State state) {
        if (state == null) { return 0; }
        return state.textObjs.Count;
    }
    
    public static string GetText (State state, int textObj_i) {
        if (state == null) { return ""; }
        if (state.textObjs.Count <= (int) textObj_i) { return ""; } // @TODO write the error to console?
        return state.textObjs[textObj_i].text;
    }
    
    public static string GetActorName (State state) {
        if (state == null) { return ""; }
        return state.actor_n;
    }
    
    public static int  GetInt (string key) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } return Vars[key].Item1; } return 0; }
    public static void SetInt (string key, int value) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } Vars[key].Item1 = value; } }
    public static int  GetInt (State state, string key) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } return Vars[key].Item1; } else { if (!state.localVars.ContainsKey(key)) { state.localVars[key] = new Pair(0, ""); } return state.localVars[key].Item1; } }
    public static void SetInt (State state, string key, int value) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } Vars[key].Item1 = value; } else { if (!state.localVars.ContainsKey(key)) { state.localVars[key] = new Pair(0, ""); } state.localVars[key].Item1 = value; } }
    
    public static string GetString (string key) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } return Vars[key].Item2; } return ""; }
    public static void   SetString (string key, string value) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } Vars[key].Item2 = value; } }
    public static string GetString (State state, string key) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } return Vars[key].Item2; } else { if (!state.localVars.ContainsKey(key)) { state.localVars[key] = new Pair(0, ""); } return state.localVars[key].Item2; } }
    public static void   SetString (State state, string key, string value) { if (Char.IsUpper(key, 0)) { if (!Vars.ContainsKey(key)) { Vars[key] = new Pair(0, ""); } Vars[key].Item2 = value; } else { if (!state.localVars.ContainsKey(key)) { state.localVars[key] = new Pair(0, ""); } state.localVars[key].Item2 = value; } }
    
    
    public static State State_I (string file_n) {
        State state = new State();
        
        string fullFile_n = file_n + ".dial";
        
        string path = Application.dataPath + "/TextContent/" + file_n + ".dial";
        //Debug.Log(path);
        TextAsset textAsset = Resources.Load<TextAsset>("TextContent/" + file_n);
        if (textAsset != null) {
        //if (File.Exists(path)) {
            //state.text = File.ReadAllText(path);
            state.text = textAsset.text;
            state.text_s = state.text.Length + 1;

            state.shouldChangeFrame = false;
            state.frames            = new List<string>();
            
            if (state.text_s < 2) {
                Error("File with the following name doesn't have at least 2 characters: " + fullFile_n);
                state = null;
                return state;
            }
            
            state.textObjs                = new List<TextObject>();
            state.jumpBasePos             = new Dictionary<int, Pos>();
            state.jumpHistory             = new List<(int,Pos)>();
            state.persCond                = new List<(string,string)>();
            state.condElse                = new List<bool>();
            state.localVars               = new Dictionary<string,Pair>();
            state.choices                 = new List<ChoiceObject>();
            state.possibleAccents         = new SortedSet<string>();
            state.hasOneUseChoiceRecurred = new Dictionary<int, bool>();
            state.condRepeat_c            = new Dictionary<int, int>();
            state.globalVarsCopy          = new Dictionary<string, Pair>();
            state.saveData                = new List<string>();
            
            state.saveData.Add("f:" + file_n);

            state.text += "\0";

            /* "removing" comments */
            bool isComment = false;
            bool isInsideDoubleSlashComment = false;
            int commentNestingDepth = 0;
            char replaceSign = '\t';
            for (int i = 0; i < state.text_s; i++) { /* @TODO check the logic here thoroughly */
                if (state.text[i] == '/' && state.text[i + 1] == '*') {
                    isComment = true;
                    commentNestingDepth++;
                    
                    char[] array = state.text.ToCharArray();
                    array[i] = replaceSign;
                    array[i + 1] = replaceSign;
                    state.text = new string(array);
                }
                if (state.text[i] == '*' && state.text[i + 1] == '/') {
                    commentNestingDepth--;
                    if (commentNestingDepth == 0 && !isInsideDoubleSlashComment) {
                        isComment = false;
                    }
                    char[] array = state.text.ToCharArray();
                    array[i] = replaceSign;
                    array[i + 1] = replaceSign;
                    state.text = new string(array);
                }
                if (state.text[i] == '/' && state.text[i + 1] == '/') {
                    isInsideDoubleSlashComment = !isInsideDoubleSlashComment; 
                    if (commentNestingDepth == 0 && !isInsideDoubleSlashComment) {
                        isComment = false;
                    }
                    char[] array = state.text.ToCharArray();
                    array[i] = replaceSign;
                    array[i + 1] = replaceSign;
                    state.text = new string(array);
                    i++;
                }

                if (isComment) {
                    char[] array = state.text.ToCharArray();
                    array[i] = replaceSign;
                    state.text = new string(array);
                }
            }
            
            state.flags = Flags.NONE;
            
            state.textWidth = DIAL_DEFAULT_TEXT_WIDTH;
            state.displayText = "";
            state.status = Status.INTERPRET;
            
            state.currentPos = new Pos();
            state.currentPos.text_i = 0;
            state.currentPos.condNestingDepth = 0;
            
            state.seedRandom = Guid.NewGuid().GetHashCode();
            state.random = new System.Random(state.seedRandom);
            state.saveData.Add("s:" + (state.seedRandom).ToString());

            //#ifdef DIAL_DEBUG
            if (HasDetectedCriticalErrors(state)) {
                state = null;
                return state;
            }
            //#endif
            LoadJumpBases(state);
            Interpret(state);
        }
        else {
            Error("Could not open a file with the following name: " + fullFile_n);
            state = null;
        }
        return state;
    }

    public static void StateSave (State state, int save_i) {
        if (state == null) { return; }
        
        string saveDataContent = "";
        foreach (var data in state.saveData) {
            saveDataContent += data + ",";
        }

        //string file_n = "save_" + state.saveData[0].Substring(2, state.saveData[1].Length - 8) + "_" + (save_i).ToString() + ".txt"; /* @TODO .dial or .txt extension thing, change the magic numbers */
        string file_n = "save_" + state.saveData[0].Substring(2) + "_" + (save_i).ToString() + ".txt";
        
        string path = Application.dataPath + "/" + file_n;
        
        File.WriteAllText(path, saveDataContent);
    }

    public static State StateLoad (string file_n, int save_i) { /* @TODO */
        State state = null;
        
        string path = Application.dataPath + "/" + "save_" + file_n + "_" + (save_i).ToString() + ".txt";
        if (File.Exists(path)) {
            string text = File.ReadAllText(path);
            int text_s = text.Length;

            text += "\0";
            
            int text_i = 0;
            
            List<string> saveData = new List<string>();
            while (true) {
                string saveDataText = "";
                while (text_i != text_s) {
                    if (text[text_i] == ',') {
                        break;
                    }
                    saveDataText += text[text_i];
                    text_i++;
                }
                if (text_i == text_s) {
                    break;
                }
                if (text[text_i] == ',') {
                    saveData.Add(saveDataText);
                    text_i++;
                }
            }
            
            Vars.Clear();
            
            state = State_I(saveData[0].Substring(2));
            
            List<string> vars = new List<string>();
            foreach (var data in saveData) {
                if (data.Length == 0) { 
                    Error("Invalid data while loading a following file: " + "save_" + file_n + "_" + (save_i).ToString() + ".txt");
                    state = null;
                    return state;
                }
                switch (data[0]) {
                    case 'f': {
                        break;
                    }
                    case 's': {
                        state.seedRandom = Convert.ToInt32(data.Substring(2));
                        state.random = new System.Random(state.seedRandom);
                        break;
                    }
                    case 'a': {
                        string accentText = data.Substring(2);
                        if (state.possibleAccents.Contains(accentText)) {
                            while (state.currentAccent.Current != accentText) {
                                state.currentAccent.MoveNext();
                            }
                        }
                        else {
                            Error("Invalid name of an accent while loading a following file: " + "save_" + file_n + "_" + (save_i).ToString() + ".txt");
                            state = null;
                            return state;
                        }
                        RefreshAccentedChoices(state);
                        break;
                    }
                    case 'v': {
                        vars.Add(data.Substring(2));
                        break;
                    }
                    case '0': {
                        foreach (var variable in vars) {
                            VarInstrInterpret(state, variable);
                        }
                        vars.Clear();
                        Continuation(state);
                        break;
                    }
                    default: {
                        bool hasSucceeded = false; int choiceNumber = stringToInt(data, ref hasSucceeded);
                        if (hasSucceeded) {
                            if (choiceNumber > 0) {
                                bool isValid = IsChoiceValid(state, choiceNumber - 1);
                                if (!isValid) { continue; }
                                Choice(state, choiceNumber - 1);
                            }
                            else {
                                Error("Negative choice number while loading a following file: " + "save_" + file_n + "_" + (save_i).ToString() + ".txt");
                                state = null;
                                return state;
                            }
                        }
                        else {
                            Error("Couldn't recognize a symbol while loading a following file: " + "save_" + file_n + "_" + (save_i).ToString() + ".txt");
                            state = null;
                            return state;
                        }
                        break;
                    }
                }
            }
            foreach (var variable in vars) {
                VarInstrInterpret(state, variable);
            }
            vars.Clear();
            Interpret(state);
        }
        else {
            Error("Could not open a file with the following name: " + "save_" + file_n + "_" + (save_i).ToString() + ".txt");
        }
        return state;
    }
    
    
    



    public static void Interpret (State state) {
        if (state == null) { return; }
        string txt = state.text;
        ref int t_i = ref state.currentPos.text_i;
        int skipCond_c = 0;
        int jumpLoop_c = 0;
        bool hasActorNameOccured = false;

        Beginning:
        
        SaveVarDiff(state);
        switch (state.status) {
            case Status.NONE: { break; }
            case Status.WAIT_FOR_CONTINUATION: { break; }
            case Status.WAIT_FOR_CHOICE: { break; }
            case Status.INTERPRET: {
            //#ifdef DIAL_DEBUG
            //    if (!isBacktrackLocked) {
            //        isBacktrackLocked = true;
            //        SaveBacktrackState(state);
            //    }
            //#endif
                int persCond_s = state.persCond.Count;
                for (int i = 0; i < persCond_s; i++) {
                    bool isConditionTrue = CondInstrInterpret(state, state.persCond[i].Item2);
                    if (isConditionTrue) {
                        JumpPointInstrInterpret(state, state.persCond[i].Item1);
                        skipCond_c = 0;
                        state.persCond.RemoveAt(i);
                        break;
                    }
                }

                while (true) {
                    switch (txt[t_i]) {
                        case '#': {
                            string instrText = ScanTextUntil(txt, ref t_i, "#");
                            VarInstrInterpret(state, instrText);
                            break;
                        }
                        case '@': {
                            string instrText = ScanTextUntil(txt, ref t_i, "@");
                            SpecInstrInterpret(state, instrText);
                            break;
                        }
                        case '$': {
                            string instrText = ScanTextUntil(txt, ref t_i, "$");
                            PersCondInstrInterpret(state, instrText);
                            break;
                        }
                        case '[': {
                            if (txt[t_i + 1] == '[') { /* [[...]], go past it */
                                SeekEndOfStatement(txt, ref t_i, "]");
                            }
                            else { /* [...] */
                                string instrText = ScanTextUntil(txt, ref t_i, "]");
                                JumpPointInstrInterpret(state, instrText);
                                skipCond_c = 0;
                                jumpLoop_c++;
                                if (jumpLoop_c >= DIAL_JUMP_LOOP_LIMIT) {
                                    Error("Infinite jump loop detected at jump point: " + instrText, txt, t_i);
                                    state.status = Status.FATAL_ERROR;
                                    goto Beginning;
                                }
                            }
                            break;
                        }
                        case ']': {
                            t_i++;
                            break;
                        }
                        case '{': {
                            t_i++;
                            SeekUntil(txt, ref t_i, "{}&");

                            if (txt[t_i] == '}') { /* {...} */
                                t_i++;
                                SeekEndOfChoiceRange(txt, ref t_i);
                            }
                            else { /* start of choice range */
                                state.choices.Clear();
                                while (true) {
                                    SeekUntil(txt, ref t_i, "{}&|");

                                    if (txt[t_i] == '{') { /* {... */
                                        int possibleBeginningOfChoice_i = t_i;
                                        t_i++;
                                        SeekUntil(txt, ref t_i, "{}");

                                        if (txt[t_i] == '{') { /* {...{ */
                                            SeekEndOfChoiceRange(txt, ref t_i); /* seek the end of this new nested choice range, so that we return to our original choice range */
                                        }
                                        else if (txt[t_i] == '}') { /* {...} */
                                            t_i = possibleBeginningOfChoice_i;
                                            string choiceInstrText = ScanTextUntil(txt, ref t_i, "}");
                                            
                                            if (!state.hasOneUseChoiceRecurred.ContainsKey(t_i)) { state.hasOneUseChoiceRecurred[t_i] = false; }
                                            if (choiceInstrText.Length != 0 && choiceInstrText[0] == '~' && state.hasOneUseChoiceRecurred[t_i]) { /* if a one-use choice has already been chosen, then it is hidden */
                                                continue;
                                            }

                                            ChoiceObject choice_b = new ChoiceObject();
                                            choice_b.instrText = choiceInstrText;
                                            choice_b.jumpPos = new Pos();
                                            choice_b.jumpPos.text_i = t_i;
                                            choice_b.jumpPos.condNestingDepth = state.currentPos.condNestingDepth;
                                            choice_b.accentedOptions = new Dictionary<string,string>();
                                            state.choices.Add(choice_b);
                                        }
                                    }
                                    else if (txt[t_i] == '}') { /* ...} */
                                        if (state.choices.Count == 0) {
                                            t_i++;
                                            break;
                                        }

                                        ChoicesInterpret(state);
                                        ShowChoices(state);

                                        state.status = Status.WAIT_FOR_CHOICE;
                                        goto Beginning;
                                    }
                                    else if (txt[t_i] == '&') { /* &...& */
                                        if (IsItConditionalChoice(txt, t_i)) {
                                            while (txt[t_i] != '{') { /* loops the conditionals */
                                                string instrText = ScanTextUntil(txt, ref t_i, "&");
                                                bool isConditionTrue = CondInstrInterpret(state, instrText);
                                                if (!state.condRepeat_c.ContainsKey(t_i)) { state.condRepeat_c[t_i] = 0; }
                                                state.condRepeat_c[t_i]++;
                                                if (isConditionTrue) {
                                                    state.currentPos.condNestingDepth++;
                                                }
                                                else {
                                                    SeekEndOfConditional(txt, ref t_i);
                                                    break;
                                                }
                                                while (IsWhitespace(txt[t_i])) {
                                                    t_i++;
                                                }
                                            }
                                        }
                                        else {
                                            SeekEndOfStatement(txt, ref t_i, "&");
                                            SeekEndOfConditional(txt, ref t_i);
                                        }
                                    }
                                    else if (txt[t_i] == '|' && txt[t_i + 1] == '~') { /* |~ */
                                        Error("The conditional's corresponding '||' symbol is outside the choice range it's in or the choice range is missing the ending '}' symbol.");
                                        break;
                                    }
                                    else if (txt[t_i] == '|' && txt[t_i + 1] == '|') { /* || */
                                        t_i += 2;
                                        state.currentPos.condNestingDepth--;

                                        if (state.currentPos.condNestingDepth < 0) {
                                            Error("Conditional nesting depth is below zero. There are stray '||' symbols or a corresponding conditional was not read properly.", txt, t_i);
                                            state.currentPos.condNestingDepth = 0;
                                        }
                                    }
                                    else if (txt[t_i] == '|') { /* | */
                                        t_i++;
                                    }
                                }
                            }
                            break;
                        }
                        case '}': {
                            t_i++;
                            break;
                        }
                        case '&': {
                            string instrText = ScanTextUntil(txt, ref t_i, "&");
                            if (IsItConditionalChoice(txt, t_i)) {
                                SeekEndOfChoiceRange(txt, ref t_i);
                            }
                            else {
                                bool isConditionTrue = CondInstrInterpret(state, instrText);
                                if (!state.condRepeat_c.ContainsKey(t_i)) { state.condRepeat_c[t_i] = 0; }
                                state.condRepeat_c[t_i]++;
                                if (isConditionTrue) {
                                    state.currentPos.condNestingDepth++;
                                    if (instrText.Length != 0 && instrText[0] == '~') {
                                        skipCond_c++;
                                    }
                                }
                                else {
                                    SeekEndOfConditional(txt, ref t_i);
                                }
                            }
                            break;
                        }
                        case '|': {
                            if (txt[t_i + 1] == '~') { /* |~ */
                                if (IsTextVisible(state.displayText)) {
                                    ShowText(state, state.displayText);
                                }
                                state.displayText = "";
                                state.status = Status.FINISHED;
                                goto Beginning;
                            }
                            else if (txt[t_i + 1] == '|') { /* || */
                                t_i += 2;
                                state.currentPos.condNestingDepth--;

                                if (state.currentPos.condNestingDepth < 0) {
                                    Error("Conditional nesting depth is below zero. There are stray '||' symbols or a corresponding conditional was not read properly.", txt, t_i);
                                    state.currentPos.condNestingDepth = 0;
                                }

                                if (skipCond_c > 0) {
                                    skipCond_c--;
                                }
                                else if (IsTextVisible(state.displayText)) {
                                    ShowText(state, state.displayText);
                                    state.displayText = "";
                                    state.status = Status.WAIT_FOR_CONTINUATION;
                                    goto Beginning;
                                }
                            }
                            else { /* | */
                                t_i++;
                                if (IsTextVisible(state.displayText)) {
                                    ShowText(state, state.displayText);
                                    state.displayText = "";
                                    state.status = Status.WAIT_FOR_CONTINUATION;
                                    goto Beginning;
                                }
                            }
                            break;
                        }
                        default: {
                            if (hasActorNameOccured == false && txt[t_i] == ':') {
                                hasActorNameOccured = true;
                                state.actor_n = RemoveWhitespace(state.displayText);
                                state.displayText = "";
                                t_i++;
                                break;
                            }
                            state.displayText += txt[t_i];
                            t_i++;
                            break;
                        }
                    }
                }
                //break;
            }
            case Status.FATAL_ERROR: {
                break;
            }
            case Status.FINISHED: {
                break;
            }
            default: break;
        }
    }
}
