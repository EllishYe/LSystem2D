using System.Collections.Generic;
using UnityEngine;

public class Test3_LsystemDrawer : MonoBehaviour
{
    public GameObject linePrefab;
    public float step = 1f;

    public GameObject leafPrefab;
    public bool generateFoliage = false;
    private List<GameObject> leaves = new List<GameObject>();

    private Vector3 position;
    private float angle;

    private List<LineRenderer> lines = new List<LineRenderer>();

    struct TurtleState
    {
        public Vector3 position;
        public float angle;
        public int branchID;           // 保存分支编号
        public TurtleState(Vector3 p, float a, int bid)
        {
            position = p;
            angle = a;
            branchID = bid;
        }
    }
    private Stack<TurtleState> stateStack = new Stack<TurtleState>();

    // 线段信息
    private struct SegmentInfo
    {
        public Vector3 endPos;
        public Vector3 dir;
        public int index;
        public int branchID;           // 当前线段属于哪个分支
    }
    private List<SegmentInfo> segments = new List<SegmentInfo>();
    private int segmentCounter = 0;

    // 每个 branchID 对应的 segment 列表
    private Dictionary<int, List<SegmentInfo>> branches = new Dictionary<int, List<SegmentInfo>>();
    private int currentBranchID = 0;
    private int nextBranchID = 1;

    public void Draw(string commands, float deltaAngle)
    {
        ClearLines();

        position = Vector3.zero;
        angle = 90f;

        currentBranchID = 0;
        nextBranchID = 1;
        branches[currentBranchID] = new List<SegmentInfo>();

        segmentCounter = 0;

        foreach (char c in commands)
        {
            if (c == 'F' || c == 'l' || c == 'r') DrawForward(true);
            else if (c == 'f') DrawForward(false);

            else if (c == '+') angle += deltaAngle;
            else if (c == '-') angle -= deltaAngle;

            else if (c == '[')
            {
                // 保存当前状态
                stateStack.Push(new TurtleState(position, angle, currentBranchID));

                // 开启新分支
                currentBranchID = nextBranchID++;
                branches[currentBranchID] = new List<SegmentInfo>();
            }

            else if (c == ']')
            {
                if (stateStack.Count > 0)
                {
                    TurtleState s = stateStack.Pop();
                    position = s.position;
                    angle = s.angle;
                    currentBranchID = s.branchID;
                }
            }
        }

        if (generateFoliage && leafPrefab != null)
            GenerateLeaves();

        CameraAutoFitZoom camFit = Camera.main.GetComponent<CameraAutoFitZoom>();
        if (camFit != null)
        {
            camFit.treeLines = lines;
            camFit.AutoFit();
        }
    }

    private void DrawForward(bool draw)
    {
        Vector3 oldPos = position;
        float rad = angle * Mathf.Deg2Rad;
        position += new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * step;

        if (draw)
        {
            GameObject lineObj = Instantiate(linePrefab);
            lineObj.transform.SetParent(this.transform, false);

            LineRenderer lr = lineObj.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, oldPos);
            lr.SetPosition(1, position);
            lines.Add(lr);

            Vector3 dir = (position - oldPos).normalized;

            SegmentInfo seg = new SegmentInfo
            {
                endPos = position,
                dir = dir,
                index = segmentCounter,
                branchID = currentBranchID
            };

            segments.Add(seg);
            branches[currentBranchID].Add(seg);
        }

        segmentCounter++;
    }

    private void ClearLines()
    {
        position = Vector3.zero;
        angle = 90f;

        foreach (var l in lines) if (l != null) Destroy(l.gameObject);
        lines.Clear();

        foreach (var leaf in leaves) if (leaf != null) Destroy(leaf);
        leaves.Clear();

        stateStack.Clear();
        segments.Clear();
        branches.Clear();

        segmentCounter = 0;
        currentBranchID = 0;
        nextBranchID = 1;
    }

    // ------------------ 叶片生成：每条分支只在末端生成叶片 ------------------
    private void GenerateLeaves()
    {
        foreach (var kv in branches)
        {
            List<SegmentInfo> branchSegs = kv.Value;
            if (branchSegs.Count == 0) continue;

            // 该 branch 的最后一段就是唯一的末端
            SegmentInfo tip = branchSegs[branchSegs.Count - 1];

            GameObject leaf = Instantiate(leafPrefab);
            leaf.transform.SetParent(this.transform, false);
            leaf.transform.position = tip.endPos;

            float angleDeg = Mathf.Atan2(tip.dir.y, tip.dir.x) * Mathf.Rad2Deg;
            leaf.transform.rotation = Quaternion.Euler(0, 0, angleDeg);

            leaves.Add(leaf);
        }
    }
}
