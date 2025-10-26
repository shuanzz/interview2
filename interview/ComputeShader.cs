//計算著色器（Compute Shader），用於處理 Boid（魚群個體）的行為和邏輯，並在 GPU 上並行計算。
//#pragma 是一個預處理指令，用於給編譯器提供信息。
//kernel 表示這個函數是計算著色器的內核函數。
//CSMain 是內核函數的名稱，Unity會調用這個函數來執行計算。
#pragma kernel CSMain
//定義每個線程組的大小為 1024，這意味著每個組將執行 1024 個線程
static const int threadGroupSize = 1024;

//定義了個 Boid 結構體，包括 Boid 的各種屬性
struct Boid
{
    float3 position;
    float3 direction;
    //當前 Boid 的群體朝向總和
    float3 flockHeading;
    //當前 Boid 感知的群體位置總和
    float3 flockCentre;
    //用於避免重疊的分離方向
    float3 separationHeading;
    //該 Boid 感知到的伙伴數量
    int numFlockmates;
};

//定義了一個可讀寫的結構體緩衝區，用於存儲所有的 Boid 實例
RWStructuredBuffer<Boid> boids;
//總的 Boid 數量
int numBoids;
//感知半徑，決定 Boid 能夠感知到多少鄰近的 Boid
float viewRadius;
//用於規避的半徑，決定 Boid 應該何時採取避讓措施
float avoidRadius;

//指定此著色器的線程組大小，表示在 x 方向上使用 threadGroupSize 個線程，y 和 z 方向為 1
//主計算函數，id 是線程的唯一 ID，使用 SV_DispatchThreadID 標識
//[numthreads(threadGroupSize, 1, 1)] 中的 threadGroupSize 用來定義每個線程組的大小，即每個線程組中可以並行執行的線程數量
[numthreads(threadGroupSize, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    //***********boidB:當前遍歷到的  boids[id.x]:目前shader正在處理的boid*************  <-------重要!!
    //循環，每個線程對應一個 Boid，檢查所有 Boid
    for (int indexB = 0; indexB < numBoids; indexB++) {
    //確保當前 Boid 不與自己比較
    if (id.x != indexB)
    {
        //獲取當前遍歷到的 Boid 的數據
        Boid boidB = boids[indexB];
        //計算當前 Boid 相對於目標 Boid 的偏移向量
        float3 offset = boidB.position - boids[id.x].position;
        //計算偏移向量的平方距離，用於後續比較
        float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;

        //如果目標 Boid 在感知半徑內
        if (sqrDst < viewRadius * viewRadius)
        {
            //增加感知到的伙伴數量
            boids[id.x].numFlockmates += 1;

            //累加伙伴的方向，用於計算平均方向
            boids[id.x].flockHeading += boidB.direction;
            //累加位置，用於計算伙伴的中心位置
            boids[id.x].flockCentre += boidB.position;
            //如果目標 Boid 在規避半徑內
            if (sqrDst < avoidRadius * avoidRadius)
            {
                //計算反向單位向量，使當前 Boid 與 boidB 保持距離 (這裡沒有依照距離算權重!)
                boids[id.x].separationHeading -= offset / (sqrDst * sqrDst);  //前面sqrDst是normalize的後面是權重(距離越近權重越大)
            }
        }
    }
}
}
