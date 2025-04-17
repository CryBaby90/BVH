using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum OctreeDebugMode
{
    AllDepth,
    TargetDepth
}

public class OctreeManager : MonoBehaviour
{
    //生成物体数量
    [Range(0, 500)] public int GenCount = 100;

    //构建深度
    [Range(0, 8)] public int BuildDepth = 3;

    //物体生成范围
    [Range(0, 300)] public int Range = 100;
    
    //根节点
    private OctreeNode Root;

    //生成的场景物体
    private List<GameObject> sceneObjects;

    #region Gizmos Octree
    //是否显示八叉树
    public bool ShowDebugOctree = true;

    //可视化类型
    public OctreeDebugMode DisplayMode = OctreeDebugMode.AllDepth;

    //可视化深度
    [Range(0, 8)] public int DisplayDepth = 3;

    //不同深度的可视化颜色
    public Color[] DisplayColors;
    #endregion

    #region Scene Query
    //是否显示场景查询的结果
    public bool ShowQueryResult = true;
    //检查点对象
    public GameObject CheckTarget;
    //场景查询的结果，邻近物体列表
    private List<GameObject> queryObjects;
    //场景查询的结果，检查点所属节点
    private OctreeNode queryNode;
    #endregion
    

    private void Start()
    {
        GenSceneObjects();
        OctreePartion();
    }

    private void Update()
    {
        if (CheckTarget != null)
        {
            var position = CheckTarget.transform.position;
            if (Root.Contains(position))
            {
                var node = QueryOctTree(position, Root);
                if (node != null)
                {
                    queryObjects = node.areaObjects;
                    queryNode = node;
                }
            }
            else
            {
                queryObjects = null;
                queryNode = null;
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (Root == null) return;

        if (ShowDebugOctree && DisplayDepth <= BuildDepth)
        {
            //显示所有深度范围
            if (DisplayMode == OctreeDebugMode.AllDepth)
            {
                Gizmos.color = new Color(1,1,1,0.2f);
                DrawNode(Root, DisplayDepth);
            }
            //只显示指定深度范围
            else if(DisplayMode == OctreeDebugMode.TargetDepth)
            {
                if (DisplayColors.Length > DisplayDepth)
                {
                    var color = DisplayColors[DisplayDepth];
                    color.a = 0.2f;
                    Gizmos.color = color;
                    DrawTargetDepth(Root, DisplayDepth);
                }
            }
        }
        
        if (ShowQueryResult)
        {
            Gizmos.color = Color.green;
            queryNode?.DrawGizmos();

            if (queryObjects != null)
            {
                Gizmos.color = Color.red;

                foreach (var obj in queryObjects)
                {
                    Gizmos.DrawWireSphere(obj.transform.position, 0.2f);
                    Gizmos.DrawLine(CheckTarget.transform.position, obj.transform.position);
                }
            }
        }
    }

    
    /// <summary>
    /// 场景查询函数
    /// 这是一个“树的遍历”算法，由于Octree的空间特性，我们有一条定理：
    ///若检查坐标在节点A中，则一定也在节点A的父节点中，一定不在A的兄弟节点内。
    /// </summary>
    private OctreeNode QueryOctTree(Vector3 position, OctreeNode checkNode)
    {
        if (checkNode.Top0?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Top0);
        if (checkNode.Top1?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Top1);
        if (checkNode.Top2?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Top2);
        if (checkNode.Top3?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Top3);

        if (checkNode.Bottom0?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Bottom0);
        if (checkNode.Bottom1?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Bottom1);
        if (checkNode.Bottom2?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Bottom2);
        if (checkNode.Bottom3?.Contains(position) ?? false) return QueryOctTree(position, checkNode.Bottom3);

        return checkNode;
    }

    //生成场景物体
    private void GenSceneObjects()
    {
        var genRange = Range * 0.5f;
        sceneObjects = new List<GameObject>();

        for (int i = 0; i < GenCount; i++)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = new Vector3(Random.Range(-genRange, genRange),
                Random.Range(-genRange, genRange),
                Random.Range(-genRange, genRange));
            obj.hideFlags = HideFlags.HideInHierarchy;
            sceneObjects.Add(obj);
        }
    }

    //进行Octree划分
    private void OctreePartion()
    {
        var initialOrigin = Vector3.zero;
        Root = new OctreeNode(initialOrigin, Range)
        {
            areaObjects = sceneObjects
        };
        GenerateOctree(Root, Range, BuildDepth);
    }

    private void GenerateOctree(OctreeNode root, float range, float depth)
    {
        if (depth <= 0) return;

        //计算grid的中心 尺寸
        var halfRange = range * 0.5f;
        var rootOffset = halfRange * 0.5f;
        var rootCenter = root.center;

        //1. 创建8个子节点
        var origin = rootCenter + new Vector3(-1, 1, -1) * rootOffset;
        root.Top0 = new OctreeNode(origin, halfRange);

        origin = rootCenter + new Vector3(1, 1, -1) * rootOffset;
        root.Top1 = new OctreeNode(origin, halfRange);

        origin = rootCenter + new Vector3(1, 1, 1) * rootOffset;
        root.Top2 = new OctreeNode(origin, halfRange);

        origin = rootCenter + new Vector3(-1, 1, 1) * rootOffset;
        root.Top3 = new OctreeNode(origin, halfRange);

        origin = rootCenter + new Vector3(-1, -1, -1) * rootOffset;
        root.Bottom0 = new OctreeNode(origin, halfRange);

        origin = rootCenter + new Vector3(1, -1, -1) * rootOffset;
        root.Bottom1 = new OctreeNode(origin, halfRange);

        origin = rootCenter + new Vector3(1, -1, 1) * rootOffset;
        root.Bottom2 = new OctreeNode(origin, halfRange);

        origin = rootCenter + new Vector3(-1, -1, 1) * rootOffset;
        root.Bottom3 = new OctreeNode(origin, halfRange);

        //2. 遍历当前空间对象，分配对象到子节点
        PartitionSceneObjects(root);

        //3. 判断子节点对象数量，如果过多，则继续递归划分。
        if (root.Top0.objectCount >= 2)
            GenerateOctree(root.Top0, halfRange, depth - 1);

        if (root.Top1.objectCount >= 2)
            GenerateOctree(root.Top1, halfRange, depth - 1);

        if (root.Top2.objectCount >= 2)
            GenerateOctree(root.Top2, halfRange, depth - 1);

        if (root.Top3.objectCount >= 2)
            GenerateOctree(root.Top3, halfRange, depth - 1);

        if (root.Bottom0.objectCount >= 2)
            GenerateOctree(root.Bottom0, halfRange, depth - 1);

        if (root.Bottom1.objectCount >= 2)
            GenerateOctree(root.Bottom1, halfRange, depth - 1);

        if (root.Bottom2.objectCount >= 2)
            GenerateOctree(root.Bottom2, halfRange, depth - 1);

        if (root.Bottom3.objectCount >= 2)
            GenerateOctree(root.Bottom3, halfRange, depth - 1);
    }

    //将空间中的物体划分到子节点
    private void PartitionSceneObjects(OctreeNode root)
    {
        var objcets = root.areaObjects;
        foreach (var obj in objcets)
        {
            if (root.Top0.Contains(obj.transform.position))
            {
                root.Top0.AddGameobject(obj);
            }
            else if (root.Top1.Contains(obj.transform.position))
            {
                root.Top1.AddGameobject(obj);
            }
            else if (root.Top2.Contains(obj.transform.position))
            {
                root.Top2.AddGameobject(obj);
            }
            else if (root.Top3.Contains(obj.transform.position))
            {
                root.Top3.AddGameobject(obj);
            }
            else if (root.Bottom0.Contains(obj.transform.position))
            {
                root.Bottom0.AddGameobject(obj);
            }
            else if (root.Bottom1.Contains(obj.transform.position))
            {
                root.Bottom1.AddGameobject(obj);
            }
            else if (root.Bottom2.Contains(obj.transform.position))
            {
                root.Bottom2.AddGameobject(obj);
            }
            else if (root.Bottom3.Contains(obj.transform.position))
            {
                root.Bottom3.AddGameobject(obj);
            }
        }
    }
    
    private void DrawTargetDepth(OctreeNode node, int depth)
    {
        if(node == null) return;

        if (depth <= 0)
        {
            node.DrawGizmos();
            return;
        }
        
        var nextDepth = depth - 1;
        var kid = node.Top0;
        DrawTargetDepth(kid, nextDepth);
        
        kid = node.Top1;
        DrawTargetDepth(kid, nextDepth);

        kid = node.Top2;
        DrawTargetDepth(kid, nextDepth);

        kid = node.Top3;
        DrawTargetDepth(kid, nextDepth);

        kid = node.Bottom0;
        DrawTargetDepth(kid, nextDepth);

        kid = node.Bottom1;
        DrawTargetDepth(kid, nextDepth);

        kid = node.Bottom2;
        DrawTargetDepth(kid, nextDepth);

        kid = node.Bottom3;
        DrawTargetDepth(kid, nextDepth);
    }
    
    private void DrawNode(OctreeNode node, int depth)
    {
        if (node == null) return;

        if (depth > 0 && depth < DisplayColors.Length)
        {
            var color = DisplayColors[depth];
            color.a = 0.2f;
            Gizmos.color = color;
            node.DrawGizmos();
        }
        
        var kid = node.Top0;
        DrawNode(kid, depth - 1);
        
        kid = node.Top1;
        DrawNode(kid, depth - 1);

        kid = node.Top2;
        DrawNode(kid, depth - 1);

        kid = node.Top3;
        DrawNode(kid, depth - 1);

        kid = node.Bottom0;
        DrawNode(kid, depth - 1);

        kid = node.Bottom1;
        DrawNode(kid, depth - 1);

        kid = node.Bottom2;
        DrawNode(kid, depth - 1);

        kid = node.Bottom3;
        DrawNode(kid, depth - 1);
    }
}