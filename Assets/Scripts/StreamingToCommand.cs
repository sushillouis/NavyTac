using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor.Rendering;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using UnityEngine.UIElements;

public delegate void SelectionDelegate(List<Entity> entities, EntityConditionDelegate conditionDelegate);
public delegate bool EntityConditionDelegate(Entity entity);

public class StreamingToCommand: MonoBehaviour 
{
    [SerializeField] Transform commandDisplay;
    [SerializeField] Transform optionsDisplay;
    [SerializeField] GameObject commandWordPrefab;
    [SerializeField] GameObject optionWordPrefab;
    [SerializeField] GameObject billboardPrefab;
    [SerializeField] GameObject currentOptionsScroll;
    [SerializeField] GameObject optionsScrollPrefab;
    [SerializeField] List<GameObject> words = new();
    [SerializeField] List<GameObject> billboards = new();
    [SerializeField] List<Entity> billboardTiedEntities = new();
    [SerializeField] List<Vector3> billboardLocations = new();
    int billboardFilter = 0;
    [SerializeField] string priorword = "";
    [SerializeField] string fullCommand = "";
    [SerializeField] Vector3 rectPos = Vector3.zero;
    bool pause = false;
    public float billboardScale = .0033f;
    public float billboardHeightScale = .25f;
    [SerializeField] float pauseTimer = 2f;
    [SerializeField] WhisperTester tester;
    public void ProcessResult(string result,int testID=-1,float time = 0f) {
        if(pause || fullCommand.Length>250) {
            return;
        }
        result = WordCleanup.StripPunctuation(result);
        string[] tokens = result.Split(' ');
        bool executeCommand = false;
        long tempNumb = -1;
        for (int i = 0;i<tokens.Length && fullCommand.Length<250;i++) {
            string word = tokens[i];
            tempNumb = WordCleanup.ConvertToNumbers(word,out bool numberFlag);
            if(long.TryParse(word, out long number)) {
                tempNumb=number;
                numberFlag=true;
            }
            if(numberFlag) {
                word = tempNumb.ToString();  
            } else if(WordCleanup.nearWords.TryGetValue(word, out string value)) {
                word = value;
            }
            // Debug.Log("Current Word-"+word+"-");
            if ((WordCleanup.CommandLanguageDict.ContainsKey(word) 
            && priorword == "" && WordCleanup.startWords.Contains(word))  
            || (WordCleanup.CommandLanguageDict.ContainsKey(priorword) 
            && (WordCleanup.CommandLanguageDict[priorword].Any(option => word == option.Item2)
            || WordCleanup.CommandLanguageDict[priorword].Any(option => option.Item2 == "NUMBERS")
            || WordCleanup.CommandLanguageDict[priorword].Any(option => option.Item2 == "LOCATIONS")))
            ) {
                if(priorword != "" && WordCleanup.CommandLanguageDict[priorword][0].Item2 == "NUMBERS" && numberFlag) {
                    executeCommand = true;
                }
                // Debug.Log(WordCleanup.CommandLanguageDict[word].ToArray());
                GameObject tempWord = Instantiate(commandWordPrefab,commandDisplay);
                words.Add(tempWord);
                RectTransform tempRect = tempWord.GetComponent<RectTransform>();
                tempRect.sizeDelta = new(40+(40*word.Length),85);
                rectPos=tempRect.position;
                TMP_Text tempText = tempWord.GetComponentInChildren<TMP_Text>();
                tempText.text = word.ToUpper();
                (int,string) pair = WordCleanup.CommandLanguageDict[priorword].Find(option => word == option.Item2);
                // Debug.Log(pair);
                if(pair.Item1 >1 ) {
                    fullCommand+=word+" ";
                    pair= WordCleanup.CommandLanguageDict[word][pair.Item1-2];
                    word = pair.Item2;
                    tempWord = Instantiate(commandWordPrefab,commandDisplay);
                    words.Add(tempWord);
                    tempRect = tempWord.GetComponent<RectTransform>();
                    tempRect.sizeDelta = new(40+(40*word.Length),85);
                    rectPos=tempRect.position;
                    tempText = tempWord.GetComponentInChildren<TMP_Text>();
                    tempText.text = word.ToUpper();
                } if(pair.Item1== -10) {
                    DisplayUnitBillboards(pair.Item2,EntityMgr.inst.entities);
                } else if(pair.Item1== -9) {
                    DisplayLocationBillboards(pair.Item2,Camera.current.transform.position);
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
            // Debug.Log(fullCommand);
            if(tester && tester.isActiveAndEnabled) {
                tester.ValidateResult(fullCommand,time,1f/Time.deltaTime,testID);
            } if(testID<0) {
                Debug.Log(tempNumb);
                ExecuteCommand(fullCommand,(int)tempNumb);
            }
            pause=true;
            Stop();
            if(testID<0) {
                Invoke(nameof(Init), pauseTimer);
            } else {
                Init();
            }
        } else if(priorword!="" && WordCleanup.CommandLanguageDict.ContainsKey(priorword)) {
            if(currentOptionsScroll!=null) {
                Destroy(currentOptionsScroll);
            }
            currentOptionsScroll = Instantiate(optionsScrollPrefab,optionsDisplay);
            FillOptions(WordCleanup.CommandLanguageDict[priorword]);
        }
    }

    public void DisplayUnitBillboards(string command, List<Entity> entities) {
        if(billboards.Count > 0) {
            CameraMgr.inst.onCameraMove.RemoveListener(UpdateBillboardSzie);
        }
        foreach (GameObject billboard in billboards)
        {
            Destroy(billboard);
        }
        billboards.Clear();
        if(entities != null && entities.Count>0) {
            billboardTiedEntities.Clear();
        }
        switch (command)
        {
            case "#":
                int i = 0;
                foreach (Entity ent in entities) {
                    GameObject billboard = Instantiate(billboardPrefab,ent.transform);
                    billboard.GetComponent<Canvas>().worldCamera = CameraMgr.inst.myCamera;
                    RectTransform tempRect = billboard.GetComponentInChildren<RectTransform>();
                    Button tempButton = billboard.GetComponentInChildren<Button>();
                    int j = i;
                    tempButton.onClick.AddListener(() => this.ProcessResult(j.ToString()));
                    tempRect.sizeDelta = new(40+(20*i),42.5f);
                    TMP_Text tempText = billboard.GetComponentInChildren<TMP_Text>();
                    tempText.text = i.ToString();
                    billboards.Add(billboard);
                    billboardTiedEntities.Add(ent);
                    i++;
                }
                break;
            default:
            break;
        }

        if(billboards.Count > 0) {
            CameraMgr.inst.onCameraMove.AddListener(UpdateBillboardSzie);
            UpdateBillboardSzie();
        }
    }

    public void DisplayLocationBillboards(string command, Vector3 cameraPos) {
        if(billboards.Count > 0) {
            CameraMgr.inst.onCameraMove.RemoveListener(UpdateBillboardSzie);
        }
        foreach (GameObject billboard in billboards)
        {
            Destroy(billboard);
        }
        billboards.Clear();
        if(command == "") {
            billboardTiedEntities.Clear();
        }
        switch (command)
        {
            case "#":
                int i = 0;
                for(int j=-10;j<11;j++) {for(int k=-10;k<11;k++) {
                    Vector3 offset = new Vector3(500*j,0,500*k);
                    GameObject billboard = Instantiate(billboardPrefab, cameraPos+offset, Quaternion.identity);
                    billboard.GetComponent<Canvas>().worldCamera = CameraMgr.inst.myCamera;
                    RectTransform tempRect = billboard.GetComponentInChildren<RectTransform>();
                    Button tempButton = billboard.GetComponentInChildren<Button>();
                    int q = i;
                    tempButton.onClick.AddListener(() => this.ProcessResult(q.ToString()));
                    tempRect.sizeDelta = new(40,42.5f);
                    TMP_Text tempText = billboard.GetComponentInChildren<TMP_Text>();
                    tempText.text = i.ToString();
                    billboards.Add(billboard);
                    billboardLocations.Add(cameraPos+offset);
                    i++;
                }}
                break;
            default:
            break;
        }

        if(billboards.Count > 0) {
            CameraMgr.inst.onCameraMove.AddListener(UpdateBillboardSzie);
            UpdateBillboardSzie();
        }
    }

    public void UpdateBillboardSzie() {
        foreach (GameObject billboard in billboards)
        {
            float distance = Vector3.Distance(CameraMgr.inst.YawNode.transform.position,billboard.transform.position);
            distance = Mathf.Clamp(distance,1f,99999f);
            billboard.transform.localScale =  billboardScale * Mathf.Sqrt(distance) * Vector3.one;
            billboard.transform.position = new(billboard.transform.position.x,60+distance*billboardHeightScale,billboard.transform.position.z);
        }
    }

    public void ExecuteCommand(string command, int id) {
        Debug.Log(command);
        System.Collections.IEnumerator tokens = command.Split(' ').GetEnumerator();
        List<Entity> importantEnts = new();
        if(!tokens.MoveNext()) return;
        SelectionDelegate selector =  SelectionMgr.inst.SelectAllEntitiesOnCondition;
        EntityConditionDelegate entityCondition = (_) => true; 
        bool locationFlag = false;
        if((string)tokens.Current=="select" || (string)tokens.Current=="group") {
            EntityClass classFilter = EntityClass.None;
            if((string)tokens.Current=="group") {
                selector = TacticalAIMgr.inst.AutoCreateBindControlGroup;
            }
            if(!tokens.MoveNext()) return;

            if((string)tokens.Current=="set") {
                if(!tokens.MoveNext()) return; 
                SelectionMgr.inst.ClearSelection();
            } else if((string)tokens.Current=="add") {
                if(!tokens.MoveNext()) return; 
            }

            if((string)tokens.Current=="all") {
                if(!tokens.MoveNext()) return; 
            } else if((string)tokens.Current=="half") {
                if(!tokens.MoveNext()) return; 
                //TODO
            }

            if((string)tokens.Current=="of"){
                if(!tokens.MoveNext()) return; 
                classFilter = EntityClass.Carrier; // Temp (find class)
                entityCondition = ent => ent.entityClass == classFilter;
            } 
            if((string)tokens.Current=="on") {
                if(!tokens.MoveNext()) return; 
            } 


            if((string)tokens.Current=="screen") {
                if(!tokens.MoveNext()) return; 
                importantEnts = SelectionMgr.inst.GetAllEntitiesOnScreen();
            } else if((string)tokens.Current=="left") {
                if(!tokens.MoveNext()) return; 
                importantEnts = SelectionMgr.inst.GetAllEntitiesOnLeftScreen();
            } else if((string)tokens.Current=="right") {
                if(!tokens.MoveNext()) return; 
                importantEnts = SelectionMgr.inst.GetAllEntitiesOnRightScreen();
            } else if((string)tokens.Current=="map") {
                if(!tokens.MoveNext()) return; 
                importantEnts = EntityMgr.inst.entities;
            } 

            selector(importantEnts,entityCondition);
        } else if((string)tokens.Current=="attack") {
            if(!tokens.MoveNext()) return;
            int dirFlag = 0;
            bool allyFlag=false;
            importantEnts = SelectionMgr.inst.selectedEntities.FindAll(ent => ent.owner == PlayerMgr.inst.localPlayer);
            if((string)tokens.Current=="furthest") {
                dirFlag = 1;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="nearest") {
                dirFlag = -1;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="specific") {
                if(!tokens.MoveNext()) return;
            }

            if((string)tokens.Current=="ally") {
                allyFlag=true;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="enemy") {
                if(!tokens.MoveNext()) return;
            }

            if((string)tokens.Current=="group") {
                allyFlag=true;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="single") {
                if(!tokens.MoveNext()) return;
            }

            if(!tokens.MoveNext()) return; // #

            ExecuteDynamicAction(importantEnts,dirFlag,allyFlag,true,locationFlag,id);
            

        } else if((string)tokens.Current=="move") {
            if(!tokens.MoveNext()) return;
            importantEnts = SelectionMgr.inst.selectedEntities.FindAll((Entity ent) => ent.owner == PlayerMgr.inst.localPlayer);
            if((string)tokens.Current=="all") {
                if(!tokens.MoveNext()) return;
                
            } else if((string)tokens.Current=="half") {
                if(!tokens.MoveNext()) return;
                //todo
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

            if((string)tokens.Current=="group") {
                allyFlag=true;
                if(!tokens.MoveNext()) return;
            } else if((string)tokens.Current=="single") {
                if(!tokens.MoveNext()) return;
            }

            if((string)tokens.Current=="location") {
                if(!tokens.MoveNext()) return;
                locationFlag = true;
            }

            ExecuteDynamicAction(importantEnts,dirFlag,allyFlag,false,locationFlag,id);

        }
    }

    public void ExecuteDynamicAction(List<Entity> importantEnts, int dirFlag, bool allyFlag, bool attackFlag, bool locationFlag, int specificID=-1) {
        TactPlayer player = PlayerMgr.inst.localPlayer;
        Vector3 center = Vector3.zero;


        // if(allyFlag && attackFlag) {
        //     Debug.LogWarning("Attacking Ally, currently not implemented");
        //     return;
        // }

        float disValue = dirFlag < 0 ? float.MaxValue : float.MinValue;
        int foundIndex = 0;
        Vector3 targetPos = Vector3.zero;
        Debug.Log(specificID);
        Entity target = null;
        if (specificID == -1)
        {
            foreach (Entity ent in importantEnts)
            {
                center += ent.position;
            }
            center /= importantEnts.Count;

            for (int i = 0; i < EntityMgr.inst.entities.Count; i++)
            {
                if ((EntityMgr.inst.entities[i].owner == player && !allyFlag) || (EntityMgr.inst.entities[i].owner != player && allyFlag))
                {
                    continue;
                }
                float dist = Vector3.Distance(center, EntityMgr.inst.entities[i].position);
                if ((dirFlag < 0 && dist < disValue) || dist > disValue)
                {
                    foundIndex = i;
                    disValue = dist;
                }
            }
            target = EntityMgr.inst.entities[foundIndex];
        }
        else if(!locationFlag)
        {
            target = billboardTiedEntities[specificID];
            if (target == null || target.health <= 0)
            {
                return; // Check for null and dead.
            }
        } else {
            targetPos = billboardLocations[specificID];
        }
        if (target!=null && target.health>0) {
            targetPos = target.position;
        } 
        if(attackFlag) {
            foreach(Entity ent in importantEnts) {
                ent.ai.StopAndRemoveAllCommands();
                ent.ai.AddCommand(new Intercept(ent,target));
            }
        } else {
            foreach(Entity ent in importantEnts) {
                ent.ai.StopAndRemoveAllCommands();
                ent.ai.AddCommand(new Move(ent,targetPos));
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
        DisplayUnitBillboards("",null);
        if(currentOptionsScroll!=null) {
            Destroy(currentOptionsScroll);
        }
        fullCommand="";
        priorword="";
    }

    public void FillOptions(List<(int,string)> options) {
        List<string> temp = new();
        options.Where((option)=> option.Item1 !=-1).ToList().ForEach(option => temp.Add(option.Item2));
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
            Button tempButton = tempWord.GetComponent<Button>();
            tempButton.onClick.AddListener(() => this.ProcessResult(option));
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
    // What the numbers mean
    // 0 means normal word
    // 1 means ending word
    // >1 means the word has an auto word after, like "to"
    //      if >1 select option [n-2] "move -> all (3) = ally[1] == "to"
    // if =-1 word is auto
    // if =-10 word is billboarded ships
    // if =-9 word is billboarded space
    // if =-5 word is waiting for click
    // if =-20 word is unit

    // NO CAPS
    public static Dictionary<string, List<(int, string)>> CommandLanguageDict = new Dictionary<string, List<(int,string)>>{{
        "select",new(){
            (1,"clear"),
            (0,"add"),
            (0,"set")
        }}, {"group", new(){
            (1,"clear"),
            (0,"add"),
            (0,"set")
        }}, {"add", new(){
            (0,"all"),
            (0,"half"),
        }}, {"set", new(){
            (0,"all"),
            (0,"half"),
        }}, {"attack", new(){
            (0,"furthest"),
            (0,"nearest"),
            (0, "specific"),
            (-5, "click")
        }}, {"move", new(){
            (2,"all"),
            (2,"half"),
        }}, {"to", new(){
            (0,"furthest"),
            (0,"nearest"),
            (0, "specific"),
            (-5, "click")
        }}, {"specific", new(){
            (0,"ally"),
            (0,"enemy"),
            (2,"location"),
        }}, {"location", new(){
            (-9,"#"),
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
            (-10,"#"),
        }}, {"all", new(){
            (-1,"to"),
            (0,"on"),
            (0,"of")
        }}, {"half", new(){
            (-1,"to"),
            (0,"on"),
            (-20,"of")
        }}, {"on", new(){
            (1,"left"),
            (1,"right"),
            (1,"screen"),
            (1,"map"),
        }}, {"of", new(){
            (0,"you shouldn't be seeing this :P"),
        }}, {"execute", new(){
            (2,"tactic"),
        }}, {"tactic", new(){
            (0,"formation"),
        }}, {"formation", new(){
            (2,"at"),
        }}, {"#", new(){
            (1,"NUMBERS"),
            (1,"LOCATIONS"),
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

        if(numbers.Count() > 0)
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