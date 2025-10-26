using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using UnityEngine;

public class Boid : MonoBehaviour
{
    //該敵人的AIData
    public AIData aiData;

    //射線開始點(head) (Hip->Spine->Neck->Head)
    public Transform head;

    // State
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Vector3 forward;
    Vector3 velocity;

    // To update:
    Vector3 acceleration;

    //這些變量用於在群體行為中計算與其夥伴之間的關係。
    /// <summary>
    /// 用來計算群體中其他Boid的全部forward向量"總和"!!!
    /// </summary>
    [HideInInspector]
    public Vector3 avgFlockHeading;
    /// <summary>
    /// 用來計算群體中"與"其他Boid的全部相斥向量"總和"!!!
    /// </summary>
    [HideInInspector]
    public Vector3 avgAvoidanceHeading;
    /// <summary>
    /// 用來計算群體中其他Boid的全部位置向量"總和"!!!
    /// </summary>
    [HideInInspector]
    public Vector3 centreOfFlockmates;

    /// <summary>
    /// 在"觀察範圍"內的夥伴數量
    /// </summary>
    [HideInInspector]
    public int numPerceivedFlockmates;

    // Cached
    Material material;

    //緩存當前 Boid 的 Transform
    Transform cachedTransform;

    /// <summary>
    /// 要追蹤的目標
    /// </summary>
    Transform target;

    void Awake()
    {
        cachedTransform = transform;
    }


    public void Initialize(Transform target)
    {
        this.target = target;
        //位置
        position = cachedTransform.position;
        //前方
        forward = cachedTransform.forward;

        float startSpeed = (aiData.minSpeed + aiData.maxSpeed) / 2;
        //計算"初始"速度
        velocity = transform.forward * startSpeed;
    }

    public void UpdateBoid()
    {
        if (aiData.gameObject == null)
        {
            return;
        }

        //cachedTransform就是transform(用引用類型存的)，用來更新位置和方向
        //移動(如果沒有在"移動的狀態"就不要群體移動!)<---但還是會計算群體移動值(上面)(平行運算沒差)
        if (aiData.m_bMove)
        {
            //Debug.Log("Boid Move");
            Vector3 acceleration = Vector3.zero;

            //如果目標存在，計算seek的力(往目標移動)
            if (target != null)
            {
                Vector3 offsetToTarget = (target.position - position);

                acceleration = SteerTowards(offsetToTarget) * aiData.targetWeight;
            }

            //如果感知到的夥伴數量不為零，計算與夥伴中心的偏差，以及平均方向的作用力（對齊、聚合和分離）
            if (numPerceivedFlockmates != 0)
            {
                //計算平均位置向量
                centreOfFlockmates /= numPerceivedFlockmates;
                //平均位置向量 - 自己的位置向量
                Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - position);

                //avgFlockHeading不用除以numPerceivedFlockmates是因為他不用算平均，知道總和後到SteerTowards去normalize就好
                Vector3 alignmentForce = SteerTowards(avgFlockHeading) * aiData.alignWeight;
                Vector3 cohesionForce = SteerTowards(offsetToFlockmatesCentre) * aiData.cohesionWeight;
                Vector3 seperationForce = SteerTowards(avgAvoidanceHeading) * aiData.seperateWeight;

                acceleration += alignmentForce;
                acceleration += cohesionForce;
                acceleration += seperationForce;
            }

            //檢查"前方"打射線有沒有障礙物
            if (IsHeadingForCollision())
            {
                //如果有才用ObstacleRays()計算碰撞避免力
                Vector3 collisionAvoidDir = ObstacleRays();
                Vector3 collisionAvoidForce = SteerTowards(collisionAvoidDir) * aiData.avoidCollisionWeight;
                acceleration += collisionAvoidForce;
            }
            //V = V0 + a * t
            velocity += acceleration * Time.deltaTime;
            //限制速度(在最大最小之間)
            float speed = velocity.magnitude;
            Vector3 dir = velocity / speed;
            speed = Mathf.Clamp(speed, aiData.minSpeed, aiData.maxSpeed);

            velocity = dir * speed;

            //--------------------------------------------------
            cachedTransform.position += velocity * Time.deltaTime;
            cachedTransform.forward = dir;
            position = cachedTransform.position;
            forward = dir;
        }
        else
        {
            //沒有觸發移動，記錄原本的位置和方向
            position = cachedTransform.position;
            forward = cachedTransform.forward;
        }

    }
    /// <summary>
    /// 用Raycast檢查"前方"有沒有障礙物，如果有，則返回true，否則返回false
    /// </summary>
    /// <returns></returns>
    bool IsHeadingForCollision()
    {
        RaycastHit hit;
        if (Physics.Raycast(head.position, aiData.boundsRadius, forward, out hit, aiData.collisionAvoidDst, aiData.obstacleMask))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 用數學模型計算射線(球型從z軸開始螺旋平均發射)，並回傳"輪到的"且"沒有偵測到碰撞物體"的向量
    /// </summary>
    /// <returns></returns>
    Vector3 ObstacleRays()
    {

        Vector3[] rayDirections = BoidHelper.directions;


        for (int i = 0; i < rayDirections.Length; i++)
        {
            //創建一個射線，起點為 Boid 的當前位置，方向為剛才計算出的世界方向
            Ray ray = new Ray(head.position, dir);
            Debug.DrawRay(ray.origin, ray.direction * aiData.collisionAvoidDst, Color.red);

            //ray：表示射線的起點和方向。
            //aiData.collisionAvoidDst：表示檢測的最大距離。
            //aiData.obstacleMask：用於過濾特定的碰撞層，只檢測我們關心的障礙物。
            //如果檢測的方向沒有障礙物，就返回該方向!
            if (!Physics.Raycast(ray, aiData.collisionAvoidDst, aiData.obstacleMask))
            {
                return dir;
            }
        }
        //如果所有射線方向上都檢測到障礙物，則方法返回 Boid 當前的前向方向（forward），這意味著 Boid 將繼續當前的運動方向，而不會改變
        return forward;
    }


    /// <summary>
    /// 計算seek的力，用ai和目標向量(值為最大速度) - ai當前速度向量 
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    Vector3 SteerTowards(Vector3 vector)
    {
        Vector3 v = vector.normalized * aiData.maxSpeed - velocity;
        return Vector3.ClampMagnitude(v, aiData.maxSteerForce);
    }
}
