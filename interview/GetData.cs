using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour
{

    const int threadGroupSize = 1024;
    //引用一個 BoidSettings 對象，其中存儲了 Boid 的各種參數（感知半徑和與友方的碰撞避免半徑）
    public BoidSettings settings;
    //引用一個計算著色器，用在 GPU 上處理 Boid 行為
    public ComputeShader compute;
    public Transform target;
    //自己
    public static BoidManager instance;

    //用於存儲場景中所有的 Boid 對象，以便後續進行管理和更新
    public Boid[] boids;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // void Start () {
    //     //查找場景中所有類型為 Boid 的對象，把它們存在 boids 陣列中
    //     boids = FindObjectsOfType<Boid> ();
    //     foreach (Boid b in boids) {

    //         //給每個 Boid 設置 target
    //         b.Initialize (target);
    //     }

    // }
    void Setboids()
    {
        //查找場景中所有類型為 Boid 的對象，把它們存在 boids 陣列中
        boids = FindObjectsOfType<Boid>();
        foreach (Boid b in boids)
        {

            //給每個 Boid 設置 target
            b.Initialize(target);
        }
    }

    //每幀都計算群體運動（但是不一定要執行）
    void Update()
    {
        Setboids();
        //場景內有 Boid 才會執行
        if (boids != null)
        {

            int numBoids = boids.Length;
            //創建一個 BoidData 結構的陣列，存每個 Boid 的數據（位置、方向等）
            var boidData = new BoidData[numBoids];
            //遍歷所有 Boid，將每個 Boid 的 position 和 forward（方向）存儲到 boidData 陣列中
            for (int i = 0; i < boids.Length; i++)
            {
                boidData[i].position = boids[i].position;
                boidData[i].direction = boids[i].forward;
            }

            //重要：boidBuffer（存所有 Boid 的矩陣）裡存 boidData（存每個 Boid 的資料的矩陣）--->矩陣 1 對 1

            //創建一個新的計算緩衝區，大小與 Boid 數量相同，用於存儲 Boid 數據
            var boidBuffer = new ComputeBuffer(numBoids, BoidData.Size);
            //將之前填充的 Boid 數據存儲到計算緩衝區中
            boidBuffer.SetData(boidData);
            //將計算緩衝區綁定到計算著色器的 boids 變量
            compute.SetBuffer(0, "boids", boidBuffer);
            //設置 Boid 的數量到計算著色器
            compute.SetInt("numBoids", boids.Length);
            //設置感知半徑
            compute.SetFloat("viewRadius", settings.perceptionRadius);
            //設置規避半徑
            compute.SetFloat("avoidRadius", settings.avoidanceRadius);
            //計算需要的線程組數量，確保足夠運行所有 Boid 計算
            int threadGroups = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
            //調度計算著色器的執行：
            //例如，如果 numBoids 是 1500，且 threadGroupSize 是 1024，計算出來的 threadGroups 將是 2，
            //因為第一個線程組可以處理 1024 個 Boid，第二個線程組可以處理剩下的 476 個 Boid。這樣可以確保所有 Boid 都能在計算著色器中被處理。
            compute.Dispatch(0, threadGroups, 1, 1);

            boidBuffer.GetData(boidData);
            //將計算得到的群體方向、中心、規避方向等數據傳遞給相應的 Boid
            for (int i = 0; i < boids.Length; i++)
            {
                boids[i].avgFlockHeading = boidData[i].flockHeading;
                boids[i].centreOfFlockmates = boidData[i].flockCentre;
                boids[i].avgAvoidanceHeading = boidData[i].separationHeading; //avoidanceHeading;
                boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;
                //Debug.Log(boids[i].aiData.m_FSMSystem.CurrentStateID);

                //決定該 AI 敵人是否可以移動，目前暫定只有追逐狀態可以移動
                switch (boids[i].aiData.m_FSMSystem.CurrentStateID)
                {
                    // case(EnemyTransitionStateTypeID.IdleStateID):
                    //     boids[i].aiData.m_bMove = false;
                    //     break;
                    case (EnemyTransitionStateTypeID.BossIdleState1ID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.BossIdleStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.EliteIdleStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.LittleMobIdleStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    // case(EnemyTransitionStateTypeID.ChaseStateID):
                    //     boids[i].aiData.m_bMove = true;
                    //     break;
                    case (EnemyTransitionStateTypeID.BossChaseState1ID):
                        boids[i].aiData.m_bMove = true;
                        break;
                    case (EnemyTransitionStateTypeID.BossChaseStateID):
                        boids[i].aiData.m_bMove = true;
                        break;
                    case (EnemyTransitionStateTypeID.EliteChaseStateID):
                        boids[i].aiData.m_bMove = true;
                        break;
                    case (EnemyTransitionStateTypeID.LittleMobChaseStateID):
                        boids[i].aiData.m_bMove = true;
                        break;
                    // case(EnemyTransitionStateTypeID.AttackStateID):
                    //     boids[i].aiData.m_bMove = false;
                    //     break;
                    case (EnemyTransitionStateTypeID.BossAttackState1ID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.BOSSAttackStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.EliteAttackStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.LittleMobAttackStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    // case(EnemyTransitionStateTypeID.DeadStateID):
                    //     boids[i].aiData.m_bMove = false;
                    //     break;
                    case (EnemyTransitionStateTypeID.BossDeadState1ID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.BossDeadStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.EliteDeadStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    case (EnemyTransitionStateTypeID.LittleMobDeadStateID):
                        boids[i].aiData.m_bMove = false;
                        break;
                    default:
                        boids[i].aiData.m_bMove = false;
                        break;
                }

                //避免死掉刪掉怪物後訪問到空，變空引用異常
                if (boids[i] != null)
                {
                    //調用每個 Boid 的更新方法以應用最新的數據                
                    boids[i].UpdateBoid(); //有觸發移動才會用上面更新的資料作移動!!!!!
                }
            }
            //boidBuffer.Release ();
        }
    }
    /// <summary>
    /// 存儲每個 Boid 的數據（位置、方向等）
    /// </summary>
    public struct BoidData
    {
        public Vector3 position;
        public Vector3 direction;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 separationHeading; //avoidanceHeading;
        public int numFlockmates;

        public static int Size
        {
            get
            {
                return sizeof(float) * 3 * 5 + sizeof(int);
            }
        }
    }
}
