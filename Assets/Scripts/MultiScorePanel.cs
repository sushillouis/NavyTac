using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioDataMgr : MonoBehaviour
{
    // Start is called before the first frame update
    [System.Serializable]
    public class ScenarioData
    {
        public int scenarioNumber;
        public int totalUnits;
        public int totalJARI;
        public int totalSeaHunter;
        public int totalDDG51;
        public int ourUnitsDestroyed;
        public int ourDestroyedJARI;
        public int ourDestroyedSeaHunter;
        public int ourDestroyedDDG51;
        public int enemyUnitsDestroyed;
        public int enemyDestroyedJARI;
        public int enemyDestroyedSeaHunter;
        public int enemyDestroyedDDG51;
        public float damageTaken;
        public float damageDealt;
        public bool winLoss;
        public float score;
        public string feedback;
    }

    public static ScenarioDataMgr inst;
    public List<ScenarioData> scenarioDataList;
    public void Awake()
    {
        inst = this;
        // OpenOceanMain.inst.MultiScoreList.SetActive(false);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
