using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;


using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
//Platform¿¡ ÀÖ´Â max_step Àº update ¿ë

public class FoodCollectorAgent : Agent
{
    FoodCollectorSettings m_FoodCollecterSettings;
    public GameObject area;
    FoodCollectorArea m_MyArea;
    bool m_Shoot;
    float m_EffectTime;
    Rigidbody m_AgentRb;
    float m_LaserLength;
    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public Material normalMaterial;
    public Material goodMaterial;
    public GameObject myLaser;
    public bool contribute;
    public bool useVectorObs;
    [Tooltip("Use only the frozen flag in vector observations. If \"Use Vector Obs\" " +
             "is checked, this option has no effect. This option is necessary for the " +
             "VisualFoodCollector scene.")]
    public bool useVectorFrozenFlag;



    //sharing related variables
    public int hunger_gauge = 1000;
    public int inventory_Max = 5;
    public int inventoryCount = 0;
    public bool flag = false;

    public int time_sticking_together = 0;
    public string collided_agent = "";
    public int step_cout = 0;

    DateTime extracTime = DateTime.Now;

    public float currentTime = 0;
    public float collisionTime = 0;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_MyArea = area.GetComponent<FoodCollectorArea>();
        m_FoodCollecterSettings = FindObjectOfType<FoodCollectorSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);
            //sensor.AddObservation(m_Frozen);
            sensor.AddObservation(m_Shoot);
        }
        else if (useVectorFrozenFlag)
        {
            //sensor.AddObservation(m_Frozen);
        }
    }

    public Color32 ToColor(int hexVal)
    {
        var r = (byte)((hexVal >> 16) & 0xFF);
        var g = (byte)((hexVal >> 8) & 0xFF);
        var b = (byte)(hexVal & 0xFF);
        return new Color32(r, g, b, 255);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        m_Shoot = false;

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;

        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;

        //Debug.Log("discreteActions[0]: " + discreteActions[0]);

        var shootCommand = discreteActions[0] > 0;
        //var shootCommand = true;

        if (shootCommand)
        {
            m_Shoot = true;
            dirToGo *= 0.5f;
            //m_AgentRb.velocity *= 0.75f;
        }
        m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);



        if (m_AgentRb.velocity.sqrMagnitude > 25f) // slow it down
        {
            m_AgentRb.velocity *= 0.95f;
        }

        if (m_Shoot)
        {

            var myTransform = transform;
            myLaser.transform.localScale = new Vector3(1f, 1f, m_LaserLength);
            var rayDir = 25.0f * myTransform.forward;
            Debug.DrawRay(myTransform.position, rayDir, Color.red, 0f, true);
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, 2f, rayDir, out hit, 25f))
            {
                if (hit.collider.gameObject.CompareTag("agent"))
                {

                    var collided_agent = hit.collider.gameObject.GetComponents<FoodCollectorAgent>();

                    m_FoodCollecterSettings.laserNum += 1;
                    if (collided_agent[0].hunger_gauge < hunger_gauge)
                    {
                        //Debug.Log("hunger than me");
                        ////if counterpart is more hungry than me
                        //if (collided_agent[0].inventoryCount < inventoryCount)
                        //{
                        //    collided_agent[0].inventoryCount += 1;
                        //    inventoryCount -= 1;
                        //    m_FoodCollecterSettings.sharingNum += 1;
                        //}
                    }

                }
            }
        }
        else
        {
            myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        }


        if (hunger_gauge < 800 && inventoryCount > 0)
        {
            //if hungry
            hunger_gauge += 200;
            inventoryCount -= 1;
            AddReward(1f);
            m_FoodCollecterSettings.totalScore += 1;
            flag = false; // if an agent eat itself, then flag turns to the original state
            //Debug.Log("eat my self");

        }
        else if (hunger_gauge == 0 && inventoryCount == 0)
        {
            if ((Time.frameCount % 100) == 0)
            {
                Debug.Log("punishment: " + this);
                AddReward(-2f);
            }


        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        MoveAgent(actionBuffers);

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        //Debug.Log("decision making");
        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[2] = -1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1;
        }

        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public override void OnEpisodeBegin()
    {
        //Unfreeze();
        //Unpoison();
        //Unsatiate();
        m_Shoot = false;
        m_AgentRb.velocity = Vector3.zero;
        myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        transform.position = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range),
            2f, Random.Range(-m_MyArea.range, m_MyArea.range))
            + area.transform.position;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

        SetResetParameters();
    }

    void OnCollisionEnter(Collision collision)
    {
        // eat the food when the agent collides with the food
        if (collision.gameObject.CompareTag("food"))
        {
            //Satiate();
            if (inventoryCount < inventory_Max)
            {
                //if inventory is empty

                inventoryCount++;
                //food disappear
                collision.gameObject.GetComponent<FoodLogic>().OnEaten();

            }
        }

        if (collision.gameObject.CompareTag("agent"))
        {

            m_FoodCollecterSettings.collisionNum += 1;
            //if counterpart is more hungry than me

            var counter_agent = collision.gameObject.GetComponents<FoodCollectorAgent>();

            var counter_agent_name = counter_agent[0].gameObject.name;
            var current_step = Academy.Instance.StepCount;

            //현재 만났을 때 시간
            var tmp = "";
            var tmp2 = "";
            tmp = DateTime.Now.ToString("HH:mm:ss");
            //currentTime = System.Convert.ToInt32(tmp);
            for (int i = 0; i < tmp.Length; i++)
            {
                if (tmp[i] != ':')
                {
                    tmp2 = tmp2 + tmp[i];
                }
            }
            currentTime = int.Parse(tmp2);

            //Debug.Log("currentTime: " + currentTime);
            Debug.Log(string.Format("agent no: {0} counter agent no: {1}, agent inv: {2} counter agent inv: {3}", this.name, counter_agent[0].name, inventoryCount, counter_agent[0].inventoryCount));
            if (collided_agent != "")
            {

                //Debug.Log(string.Format("step_cout: {0} current_step: {1}", step_cout, current_step));
                //방금 전에 누굴 만났어
                //if (current_step >= step_cout && current_step <= step_cout + 10)
                if (currentTime >= collisionTime && currentTime <= collisionTime + 1)
                {
                    //만난지 얼마 안됐어 -> 뭉쳐있을 가능성이 높음
                    step_cout = Academy.Instance.StepCount; //step count update cause they can meet again in a while
                    tmp = "";
                    tmp2 = "";
                    tmp = DateTime.Now.ToString("HH:mm:ss");
                    //currentTime = System.Convert.ToInt32(tmp);
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        if (tmp[i] != ':')
                        {
                            tmp2 = tmp2 + tmp[i];
                        }
                    }
                    collisionTime = int.Parse(tmp2);
                    time_sticking_together += 1;
                    m_FoodCollecterSettings.stickingTime += 1;
                    Debug.Log(string.Format("currentTime: {0} collisionTime: {1}", currentTime, collisionTime));


                }
                else
                {
                    //만난지 오래됐어
                    //initializing
                    //Debug.Log("initializing"+this);
                    collided_agent = counter_agent_name;
                    //when they met
                    step_cout = Academy.Instance.StepCount; //만났을 때의 step count
                    time_sticking_together = 0;
                    tmp = "";
                    tmp2 = "";
                    tmp = DateTime.Now.ToString("HH:mm:ss");
                    //currentTime = System.Convert.ToInt32(tmp);
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        if (tmp[i] != ':')
                        {
                            tmp2 = tmp2 + tmp[i];
                        }
                    }
                    collisionTime = int.Parse(tmp2);

                }
            }
            else
            {
                //지금껏 누굴 만난적이 없는 애야
                collided_agent = counter_agent_name;
                //when they met
                step_cout = Academy.Instance.StepCount;
                time_sticking_together = 0;


                tmp = "";
                tmp2 = "";
                tmp = DateTime.Now.ToString("HH:mm:ss");
                //currentTime = System.Convert.ToInt32(tmp);
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (tmp[i] != ':')
                    {
                        tmp2 = tmp2 + tmp[i];
                    }
                }

                collisionTime = int.Parse(tmp2);



            }
            //sharing logic
            if (inventoryCount > 0)
            {
               if (counter_agent[0].hunger_gauge < hunger_gauge && counter_agent[0].inventoryCount < inventoryCount)
                {
                    if (counter_agent[0].inventoryCount < inventory_Max)
                    {
                        Debug.Log("share");
                        Debug.Log(string.Format("agent no: {0} counter agent no: {1}, agent inv: {2} counter agent inv: {3}", this.name, counter_agent[0].name, inventoryCount,counter_agent[0].inventoryCount));
                        AddReward(0.5f); //half reward of eating by themselves
                        counter_agent[0].inventoryCount += 1;
                        inventoryCount -= 1;
                        m_FoodCollecterSettings.sharingNum += 1;
                    }

                }
                else
                {
                    Debug.Log("you are not hungry than me");
                }
            }
        }
        Debug.Log("time_sticking_together: " + time_sticking_together);
        //once they be separate set zero

    }

    public void SetLaserLengths()
    {
        m_LaserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
    }

    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void SetResetParameters()
    {
        SetLaserLengths();
        SetAgentScale();
    }
}
