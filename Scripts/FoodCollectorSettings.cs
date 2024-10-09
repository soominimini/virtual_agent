using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using System.Diagnostics;
using UnityEditor;

public class FoodCollectorSettings : MonoBehaviour
{
    [HideInInspector]
    public GameObject[] agents;
    [HideInInspector]
    public FoodCollectorArea[] listArea;

    public int totalScore;
    public Text scoreText;

    public FoodCollectorAgent[] listAgent;

    public int laserNum;
    public Text laserText;

    public int collisionNum;
    public Text collisionText;


    public int sharingNum;
    public Text sharingText;

    public int stickingTime;
    public Text stickingText;

    StatsRecorder m_Recorder;
    StatsRecorder m_Recorder_laser;
    StatsRecorder m_Recorder_collision;
    StatsRecorder m_Recorder_sharing;
    StatsRecorder m_Recorder_sticking;
    StatsRecorder m_Recorder_step;

    Stopwatch watch = new Stopwatch();

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;


        m_Recorder_laser = Academy.Instance.StatsRecorder;
        m_Recorder_collision = Academy.Instance.StatsRecorder;
        m_Recorder_collision = Academy.Instance.StatsRecorder;
        m_Recorder_sharing = Academy.Instance.StatsRecorder;
        m_Recorder_sticking = Academy.Instance.StatsRecorder;


    }

    void EnvironmentReset()
    {
        ClearObjects(GameObject.FindGameObjectsWithTag("food"));
        //ClearObjects(GameObject.FindGameObjectsWithTag("badFood"));

        agents = GameObject.FindGameObjectsWithTag("agent");
        var area = GameObject.FindGameObjectsWithTag("area");
        listArea = FindObjectsOfType<FoodCollectorArea>();
        foreach (var fa in listArea)
        {
            fa.ResetFoodArea(agents);
            //fa.AreaInfo(area);

        }

        totalScore = 0;

        laserNum = 0;
        collisionNum = 0;
        sharingNum = 0;
        stickingTime = 0;

        watch.Start();



    }

    void ClearObjects(GameObject[] objects)
    {
        foreach (var food in objects)
        {
            Destroy(food);
        }
    }

    public void Update()
    {

        scoreText.text = $"Score: {totalScore}";
        laserText.text = $"Score: {laserNum}";
        collisionText.text = $"Score: {collisionNum}";
        sharingText.text = $"Score: {sharingNum}";
        stickingText.text = $"Score: {stickingTime}";
        var current_step = Academy.Instance.StepCount;
        //Debug.Log(string.Format("current_step: "+current_step));
        //Debug.Log("current_step: " + current_step);
        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0)
        {
            m_Recorder.Add("TotalScore", totalScore);

            m_Recorder_laser.Add("laserNum", laserNum);
            m_Recorder_collision.Add("collisionNum", collisionNum);
            m_Recorder_sharing.Add("sharingNum", sharingNum);
            m_Recorder_sticking.Add("stickingTime", stickingTime);
        }
        listAgent = FindObjectsOfType<FoodCollectorAgent>();
        foreach (var agent in listAgent)
        {
            if (agent.hunger_gauge > 0)
                agent.hunger_gauge -= 2;
        }

        // code for stop after 1min play
        //if (watch.ElapsedMilliseconds > 60000)
        //{
        //    watch.Stop();
        //    UnityEngine.Debug.Log(watch.ElapsedMilliseconds + " ms");
        //    EditorApplication.isPaused = true;
        //}
    }
}
