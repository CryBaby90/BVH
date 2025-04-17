using System.Collections.Generic;
using UnityEngine;

public class OctreeNode
{
    //包含的物体
    public List<GameObject> areaObjects;

    //中心
    public Vector3 center;

    //尺寸
    public float size;

    //获取当前空间内的物体数量
    public int objectCount => areaObjects?.Count ?? 0;

    private const int kidCount = 8;
    private OctreeNode[] kids;

    #region Nodes

    public OctreeNode Top0
    {
        get { return kids[0]; }
        set { kids[0] = value; }
    }

    public OctreeNode Top1
    {
        get { return kids[1]; }
        set { kids[1] = value; }
    }

    public OctreeNode Top2
    {
        get { return kids[2]; }
        set { kids[2] = value; }
    }

    public OctreeNode Top3
    {
        get { return kids[3]; }
        set { kids[3] = value; }
    }

    public OctreeNode Bottom0
    {
        get { return kids[4]; }
        set { kids[4] = value; }
    }

    public OctreeNode Bottom1
    {
        get { return kids[5]; }
        set { kids[5] = value; }
    }

    public OctreeNode Bottom2
    {
        get { return kids[6]; }
        set { kids[6] = value; }
    }

    public OctreeNode Bottom3
    {
        get { return kids[7]; }
        set { kids[7] = value; }
    }

    #endregion

    public OctreeNode(Vector3 center, float size)
    {
        this.center = center;
        this.size = size;

        kids = new OctreeNode[kidCount];
        areaObjects = new List<GameObject>();
    }

    public void DrawGizmos()
    {
        Gizmos.DrawWireCube(center, Vector3.one * size);
    }

    //判断是否包含某个点
    public bool Contains(Vector3 position)
    {
        var halfSize = size * 0.5f;
        return Mathf.Abs(position.x - center.x) <= halfSize 
               && Mathf.Abs(position.y - center.y) <= halfSize
               && Mathf.Abs(position.z - center.z) <= halfSize;
    }

    public void ClearArea()
    {
        this.areaObjects?.Clear();
    }

    public void AddGameobject(GameObject obj)
    {
        areaObjects.Add(obj);
    }
}