using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoidHelper
{

    //定義了視角方向的數量，這裡設定為 300，指的是 Boid 將在這些方向上進行射線檢測或感知。
    const int numViewDirections = 300;
    //聲明了一個靜態的只讀 Vector3 陣列，用於存儲所有可能的視角方向。在整個遊戲中，所有的 Boid 實例共享這個陣列，可以幫助提高效率和節省內存
    public static readonly Vector3[] directions;
    //靜態構造函數
    static BoidHelper()
    {
        //初始化 directions 陣列
        directions = new Vector3[BoidHelper.numViewDirections];
        //goldenRatio：這裡計算了黃金比例，約為 1.6180339887。這是一個在自然界和藝術中常見的比例，有助於模擬自然樣式的排列。 
        //angleIncrement：通過黃金比例生成的角度增量，整個計算過程構建出一個均勻分布的方向。
        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        float angleIncrement = 2 * Mathf.PI * goldenRatio;

        for (int i = 0; i < numViewDirections; i++)
        {
            //歸一化索引值，將 i 轉換為 0 到 1 之間的值，以此計算傾斜度和方位角
            float t = (float)i / numViewDirections;
            //通過反餘弦函數計算傾斜角。這確保了方向的均勻分布(1 - 2 * t是-1~1的值，剛好是cos0~180的範圍)
            float inclination = Mathf.Acos(1 - 2 * t);
            //根據黃金比例為每個方向計算方位角
            float azimuth = angleIncrement * i;
            //使用計算出的 inclination 和 azimuth 生成方向的三維坐標 (x, y, z)。
            //這個計算使用了三角函數，結果是單位球面上均勻分布的點。
            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);
            directions[i] = new Vector3(x, y, z);
        }
    }

}