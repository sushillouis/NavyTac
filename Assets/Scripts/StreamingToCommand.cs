using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor.Rendering;
using UnityEditor.SearchService;
using UnityEngine;

public class StreamingToCommand: MonoBehaviour 
{
    [SerializeField] Transform commandDisplay;
    [SerializeField] Transform optionsDisplay;
    [SerializeField] GameObject commandWordPrefab;
    [SerializeField] GameObject optionWordPrefab;
    [SerializeField] GameObject currentOptionsScroll;
    [SerializeField] GameObject optionsScrollPrefab;
    [SerializeField] List<GameObject> words = new();
    [SerializeField] string priorword = "";
    [SerializeField] string fullCommand = "";
    [SerializeField] Vector3 rectPos = Vector3.zero;
    bool pause = false;
    [SerializeField] float pauseTimer = 2f;
    [SerializeField] WhisperTester tester;
    public void ProcessResult(string result,int testID=-1,float time = 0f) {
        if(pause || fullCommand.Length>250) {
            return;
        }
        result = WordCleanup.StripPunctuation(result);
        string[] tokens = result.Split(' ');
        bool executeCommand = false;
        for (int i = 0;i<tokens.Length && fullCommand.Length<250;i++) {
            string word = tokens[i];
            long temp = WordCleanup.ConvertToNumbers(word,out bool flag);
            if(flag) {
                word = temp.ToString();
            }
            if(WordCleanup.nearWords.TryGetValue(word, out string value)) {
                word = value;
            }
            Debug.Log("Current Word-"+word+"-");
            if ((WordCleanup.CommandLanguageDict.ContainsKey(word) 
            && priorword == "" && WordCleanup.startWords.Contains(word))  
            || (WordCleanup.CommandLanguageDict.ContainsKey(priorword) 
            && WordCleanup.CommandLanguageDict[priorword].Any(option => word == option.Item2))) {
                // Debug.Log(WordCleanup.CommandLanguageDict[word].ToArray());
                GameObject tempWord = Instantiate(commandWordPrefab,commandDisplay);
                words.Add(tempWord);
                RectTransform tempRect = tempWord.GetComponent<RectTransform>();
                tempRect.sizeDelta = new(40+(40*word.Length),85);
                rectPos=tempRect.position;
                TMP_Text tempText = tempWord.GetComponentInChildren<TMP_Text>();
                tempText.text = word.ToUpper();
                (int,string) pair = WordCleanup.CommandLanguageDict[priorword].Find(option => word == option.Item2);
                Debug.Log(pair);
                if(pair.Item1 >1 ) {
                    fullCommand+=word+" ";
                    word= WordCleanup.CommandLanguageDict[word][pair.Item1-2].Item2;
                    tempWord = Instantiate(commandWordPrefab,commandDisplay);
                    words.Add(tempWord);
                    tempRect = tempWord.GetComponent<RectTransform>();
                    tempRect.sizeDelta = new(40+(40*word.Length),85);
                    rectPos=tempRect.position;
                    tempText = tempWord.GetComponentInChildren<TMP_Text>();
                    tempText.text = word.ToUpper();
                }
                priorword = word;
                fullCommand+=word+" ";
                if(!WordCleanup.CommandLanguageDict.ContainsKey(word) || pair.Item1 == 1) {
                    executeCommand=true;
                    break;
                }
            }
        }
        if(executeCommand || testID>=0) {
            if(currentOptionsScroll!=null) {
                Destroy(currentOptionsScroll);
            }
            Debug.Log(fullCommand);
            tester.ValidateResult(fullCommand,testID);
            tester.LogTime(time,1f/Time.deltaTime,testID);
            if(testID<0) {
                ExecuteCommand(fullCommand);
            }
            pause=true;
            Stop();
            Invoke(nameof(Init), pauseTimer);
        } else if(priorword!="" && WordCleanup.CommandLanguageDict.ContainsKey(priorword)) {
            if(currentOptionsScroll!=null) {
                Destroy(currentOptionsScroll);
            }
            currentOptionsScroll = Instantiate(optionsScrollPrefab,optionsDisplay);
            FillOptions(WordCleanup.CommandLanguageDict[priorword]);
        }
    }

    public void ExecuteCommand(string command) {
        System.Collections.IEnumerator tokens = command.Split(' ').GetEnumerator();
        List<Entity> importantEnts = new();
        if(!tokens.MoveNext()) return;
        if((string)tokens.Current=="select") {
            if(!tokens.MoveNext()) return;
            if((string)tokens.Current=="clear") {
                SelectionMgr.inst.ClearSelection();
            }
            if((string)tokens.Current=="all") {
                if(!tokens.MoveNext()) return; // "on"
                if(!tokens.MoveNext()) return;
                if((string)tokens.Current=="screen") {
                    SelectionMgr.inst.SelectEntitiesInBox(Vector3.zero,new(9999,9999,0));
                }
            }
        } else if((string)tokens.Current=="attack") {
            if(!tokens.MoveNext()) return;
            int dirFlag = 0;
            bool allyFlag=false;
            importantEnts = EntityMgr.inst.entities.FindAll((Entity ent) => ent.owner == PlayerMgr.inst.localPlayer);
            if((string)tokens.Current=="furthest") {
                dirFlag = 1;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="nearest") {
                dirFlag = -1;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="specific") {
                if(!tokens.MoveNext()) return;
            }

            ExecuteDynamicAction(importantEnts,dirFlag,allyFlag,true);
            

        } else if((string)tokens.Current=="move") {
            if(!tokens.MoveNext()) return;
            if((string)tokens.Current=="all") {
                if(!tokens.MoveNext()) return;
                importantEnts = EntityMgr.inst.entities.FindAll((Entity ent) => ent.owner == PlayerMgr.inst.localPlayer);
            } else if((string)tokens.Current=="selected") {
                if(!tokens.MoveNext()) return;
                importantEnts = SelectionMgr.inst.selectedEntities;
            }

            if(!tokens.MoveNext()) return; // "to"
            int dirFlag = 0;
            bool allyFlag=false;
            if((string)tokens.Current=="furthest") {
                dirFlag = 1;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="nearest") {
                dirFlag = -1;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="specific") {
                if(!tokens.MoveNext()) return;
            }

            ExecuteDynamicAction(importantEnts,dirFlag,allyFlag,false);

        }
    }

    public void ExecuteDynamicAction(List<Entity> importantEnts, int dirFlag, bool allyFlag, bool attackFlag) {
        TactPlayer player = PlayerMgr.inst.localPlayer;
        Vector3 center = Vector3.zero;

        foreach(Entity ent in importantEnts) {
            center += ent.position;
        }
        center/=importantEnts.Count;

        if(allyFlag && attackFlag) {
            Debug.LogWarning("Attacking Ally, currently not implemented");
            return;
        }

        float disValue = dirFlag < 0 ? float.MaxValue : float.MinValue;
        int foundIndex = 0;

        for(int i =0; i<EntityMgr.inst.entities.Count;i++) {
            if((EntityMgr.inst.entities[i].owner==player && !allyFlag) || (EntityMgr.inst.entities[i].owner!=player && allyFlag)) {
                continue;
            }
            float dist = Vector3.Distance(center,EntityMgr.inst.entities[i].position);
            if((dirFlag<0 && dist<disValue) || dist>disValue) {
                foundIndex = i;
                disValue=dist;
            }
        }
        if(attackFlag) {
            foreach(Entity ent in importantEnts) {
                ent.ai.StopAndRemoveAllCommands();
                ent.ai.AddCommand(new Intercept(ent,EntityMgr.inst.entities[foundIndex]));
            }
        } else {
            foreach(Entity ent in importantEnts) {
                ent.ai.StopAndRemoveAllCommands();
                ent.ai.AddCommand(new Move(ent,EntityMgr.inst.entities[foundIndex].position));
            }
        }
    }

    public void Init() {
        if(currentOptionsScroll!=null) {
            Destroy(currentOptionsScroll);
        }
        foreach (GameObject word in words) {
            Destroy(word);
        }
        words.Clear();
        currentOptionsScroll = Instantiate(optionsScrollPrefab,optionsDisplay);
        FillOptions(WordCleanup.startWords);
        pause=false;
    }

    public void Stop() {
        if(currentOptionsScroll!=null) {
            Destroy(currentOptionsScroll);
        }
        fullCommand="";
        priorword="";
    }

    public void FillOptions(List<(int,string)> options) {
        List<string> temp = new();
        options.ForEach(option => temp.Add(option.Item2));
        FillOptions(temp);
    }
    
    public void FillOptions(List<string> options) {
        if(options.Count==0) {
            return;
        } 
        int maxLength = -1;
        OptionsScroll optionsScroll = currentOptionsScroll.GetComponent<OptionsScroll>();
        foreach (string option in options) {
            if(option.Length > maxLength) {
                maxLength = option.Length;
            }

        }
        float baseWidth = 40+(20*maxLength);
        foreach (string option in options) {
            GameObject tempWord = Instantiate(optionWordPrefab,optionsScroll.content);
            RectTransform tempRect = tempWord.GetComponent<RectTransform>();
            tempRect.sizeDelta = new(baseWidth,42.5f);
            TMP_Text tempText = tempWord.GetComponentInChildren<TMP_Text>();
            tempText.text = option.ToUpper();
        }

        RectTransform optionsRect = optionsScroll.content.GetComponent<RectTransform>();
        optionsRect.sizeDelta = new(0,42.5f*options.Count);
        optionsRect = optionsScroll.viewPort.GetComponent<RectTransform>();
        int viewHeight = options.Count>2 ? 2 : options.Count; 
        optionsRect.sizeDelta = new(baseWidth,42.5f*viewHeight);;
        currentOptionsScroll.GetComponent<RectTransform>().sizeDelta=new(baseWidth+20,42.5f*viewHeight);;
    }

    // public GameObject MakeCommandWord(string command, Transform parent) {

    // }
    
}

static class WordCleanup  
{  
    public static Dictionary<string, List<(int, string)>> CommandLanguageDict = new Dictionary<string, List<(int,string)>>{{
        "select",new(){
            (1,"clear"),
            (2,"1/2"),
            (2,"all")
        }}, {"attack", new(){
            (0,"furthest"),
            (0,"nearest"),
            (0, "specific")
        }}, {"move", new(){
            (3,"all"),
            (2,"selected"),
        }}, {"selected", new(){
            (-1,"to"),
        }}, {"to", new(){
            (0,"furthest"),
            (0,"nearest"),
            (0, "specific")
        }}, {"specific", new(){
            (0,"ally"),
            (0,"enemy"),
        }}, {"group", new(){
            (1,"selected"),
            (1,"unselected"),
        }}, {"furthest", new(){
            (1,"ally"),
            (1,"enemy"),
        }}, {"nearest", new(){
            (1,"ally"),
            (1,"enemy"),
        }}, {"ally", new(){
            (1,"group"),
            (2,"single"),
        }}, {"enemy", new(){
            (1,"group"),
            (2,"single"),
        }}, {"single", new(){
            (-1,"#"),
        }}, {"all", new(){
            (-1,"on"),
            (-1,"to")
        }}, {"1/2", new(){
            (-1,"on"),
        }}, {"on", new(){
            (1,"left"),
            (1,"right"),
            (1,"screen"),
        }}, {"execute", new(){
            (2,"tactic"),
        }}, {"tactic", new(){
            (0,"formation"),
        }}, {"", new(){
        }}
        // , {"formation", new(){
        //      "attack",
        //      "move"
        // }}
    };

    public static List<string> startWords = new (){
        "select",
        "attack",
        "move",
        "group"
    };
    private static Dictionary<string, long> numberTable = new Dictionary<string, long>{  
        {"zero",0},{"one",1},{"two",2},{"three",3},{"four",4},{"five",5},{"six",6},  
        {"seven",7},{"eight",8},{"nine",9},{"ten",10},{"eleven",11},{"twelve",12},  
        {"thirteen",13},{"fourteen",14},{"fifteen",15},{"sixteen",16},{"seventeen",17},  
        {"eighteen",18},{"nineteen",19},{"twenty",20},{"thirty",30},{"forty",40},  
        {"fifty",50},{"sixty",60},{"seventy",70},{"eighty",80},{"ninety",90},  
        {"hundred",100},{"thousand",1000},{"lakh",100000},{"million",1000000},  
        {"billion",1000000000},{"trillion",1000000000000},{"quadrillion",1000000000000000},  
        {"quintillion",1000000000000000000}  
    };  

    public static Dictionary<string,string> nearWords = new() {
        {"father","furthest"},
        {"nami","enemy"},
        {"number","#"},
        {"pound","#"},
        {"oh","all"},
        {"half","1/2"},
        {"bye","ally"},
        {"green","screen"},
        {"clean","screen"},
        {"unsolveable","unselected"},
    };
  
    public static long ConvertToNumbers(string numberString, out bool flag)   {  
        var numbers = Regex.Matches(numberString, @"\w+").Cast<Match>()  
                .Select(m => m.Value.ToLowerInvariant())  
                .Where(v => numberTable.ContainsKey(v))  
                .Select(v => numberTable[v]);  
        long acc = 0, total = 0L;  

        if(numbers.Count<long>() > 0)
            flag=true;
        else
            flag=false;

        foreach (var n in numbers)  
        {  
            if (n >= 1000)  
            {  
                total += acc * n;  
                acc = 0;  
            }  
            else if (n >= 100)  
            {  
                acc *= n;  
            }  
            else acc += n;  
        }  
        return (total + acc) * (numberString.StartsWith("minus",  
                StringComparison.InvariantCultureIgnoreCase) ? -1 : 1);  
    } 

    public static string StripPunctuation(string strip) {
        strip=strip.ToLower();
        var sb = new StringBuilder();

        foreach (char c in strip)
        {
        if (!char.IsPunctuation(c))
            sb.Append(c);
        }

        strip = sb.ToString();
        strip=strip.Trim();
        return strip;
    } 
} 