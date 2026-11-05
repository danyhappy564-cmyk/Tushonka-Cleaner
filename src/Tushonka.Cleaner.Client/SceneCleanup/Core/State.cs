// SPDX-License-Identifier: GPL-3.0-only
using System;

namespace Tushonka.Cleaner.SceneCleanup
{
    internal sealed class WorkBudget
    {
        private int frame=int.MinValue;
        private long spent,limit;
        public long Open(int frameNumber,long allowedTicks)
        {
            if(frame!=frameNumber) {frame=frameNumber;spent=0;}
            limit=Math.Max(0,allowedTicks);
            return Math.Max(0,limit-spent);
        }
        public void Spend(long ticks) {spent+=Math.Max(0,ticks);}
    }
    internal static class DistancePolicy
    {
        public static float Range(float distance,float referenceFov,float currentFov)
        {
            if(!(currentFov>1 && currentFov<179)) return distance*12;
            double ratio=Math.Tan(referenceFov*Math.PI/360)/Math.Tan(currentFov*Math.PI/360);
            return distance*(float)Math.Max(1,Math.Min(12,ratio));
        }
        // 스캔 때 저장한 상자까지의 거리입니다. 숨긴 물체의 Renderer.bounds는 믿을 수 없고 네이티브 호출도 없앱니다.
        public static float SquaredToBox(float px,float py,float pz,float cx,float cy,float cz,float ex,float ey,float ez)
        {
            float dx=Math.Max(Math.Abs(px-cx)-ex,0),dy=Math.Max(Math.Abs(py-cy)-ey,0),dz=Math.Max(Math.Abs(pz-cz)-ez,0);
            return dx*dx+dy*dy+dz*dz;
        }
        public static bool Hidden(float distanceSquared,float range,bool wasHidden,bool validCamera)
        {
            if(!validCamera) return false;
            float threshold=range*(wasHidden ? .9f:1.1f);
            return distanceSquared>threshold*threshold;
        }
    }
    // 강한 참조를 포함한 슬롯을 Clear에서 전부 지웁니다. 용량 초과 시 새 물체는 변경하지 않습니다.
    internal sealed class ReferenceLedger<T,TState> where T:class
    {
        private readonly T[] targets;
        private readonly TState[] originals;
        private readonly int[] categories;
        public int Count { get; private set; }
        public ReferenceLedger(int capacity) { targets=new T[capacity]; originals=new TState[capacity]; categories=new int[capacity]; }
        public bool Add(T target,TState original,int category)
        {
            if(Count==targets.Length) return false;
            targets[Count]=target; originals[Count]=original; categories[Count]=category; ++Count; return true;
        }
        public T Target(int index) { return targets[index]; }
        public TState Original(int index) { return originals[index]; }
        public int Category(int index) { return categories[index]; }
        public void Clear()
        {
            Array.Clear(targets,0,Count); Array.Clear(originals,0,Count); Array.Clear(categories,0,Count); Count=0;
        }
        internal int DebugOccupiedSlots
        {
            get { int n=0; for(int i=0;i<targets.Length;i++) if(targets[i]!=null) ++n; return n; }
        }
    }
    public struct SceneCleanupStatus
    {
        public SceneCleanupLevel Level;
        public int Candidates, ScannedNodes, LitterHidden, RubbleHidden, CasingsHidden, FoodHidden, PuddlesHidden, GlassHidden, DecalsSkipped;
        public float LastApplyMilliseconds;
        public bool Scanning, RaidActive, DecalsApplyNextRaid, CapacityReached;
    }
}
