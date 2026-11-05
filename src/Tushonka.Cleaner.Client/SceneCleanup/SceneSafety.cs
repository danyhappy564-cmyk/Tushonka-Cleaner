// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tushonka.Cleaner.SceneCleanup
{
    // 잎 물체와 형제는 허용 목록 밖의 컴포넌트를 모두 보호합니다.
    // 조상은 게임 코드로 역할을 확인한 맵 컨테이너(ContainerRules)만 추가로 통과시킵니다.
    // 상호작용 물체·콜라이더·처음 보는 타입이 있는 조상은 그대로 보호합니다.
    internal sealed class SceneSafety
    {
        private readonly List<Component> components=new List<Component>(128);
        private readonly Dictionary<Type,ContainerKind> kinds=new Dictionary<Type,ContainerKind>(64);
        private readonly Dictionary<int,bool> loadingParents=new Dictionary<int,bool>(4096);
        internal bool PlainNode(Transform node, bool allowDecal)
        {
            node.GetComponents(components);
            bool safe=components.Count<=128;
            for(int i=0;i<components.Count && safe;i++)
            {
                Component c=components[i];
                safe=c!=null && (IsPlain(c) || (allowDecal && c is StaticDeferredDecal));
            }
            components.Clear();
            return safe;
        }
        private static bool IsPlain(Component c) { return c is Transform || c is MeshFilter || c is MeshRenderer || c is LODGroup; }
        private ContainerKind KindOf(Type type)
        {
            ContainerKind kind;
            if(!kinds.TryGetValue(type,out kind)) { kind=ContainerRules.Kind(type.FullName); kinds.Add(type,kind); }
            return kind;
        }
        private bool ContainerNode(Transform node,out bool groupRoot)
        {
            groupRoot=false;
            node.GetComponents(components);
            bool safe=components.Count<=128;
            for(int i=0;i<components.Count && safe;i++)
            {
                Component c=components[i];
                if(c==null) { safe=false; break; }
                if(IsPlain(c)) continue;
                ContainerKind kind=KindOf(c.GetType());
                safe=kind!=ContainerKind.None;
                groupRoot|=kind==ContainerKind.GroupRoot;
            }
            components.Clear();
            return safe;
        }
        // 그룹 루트는 씬 단위 묶음입니다. 그 위는 물체가 아니라 맵 구조이므로 더 올라가지 않습니다.
        private bool ContainerChainSafe(Transform first)
        {
            int depth=0;
            for(Transform t=first;t!=null;t=t.parent)
            {
                bool groupRoot;
                if(++depth>64 || !ContainerNode(t,out groupRoot)) return false;
                if(groupRoot) return true;
            }
            return true;
        }
        internal bool AncestorsSafe(Transform node,bool allowDecal)
        {
            return PlainNode(node,allowDecal) && ContainerChainSafe(node.parent);
        }
        // 근접 그룹의 형제 콜라이더도 확인합니다. 검사량이 큰 그룹은 숨기지 않습니다.
        // 형제 수 32개·조상 깊이 64개를 넘으면 변경하지 않습니다.
        internal bool SmallGroupSafe(Transform leaf,bool allowDecal)
        {
            if(!AncestorsSafe(leaf,allowDecal)) return false;
            var group=leaf.parent;
            if(group==null) return leaf.childCount==0;
            if(group.childCount>32) return false;
            for(int i=0;i<group.childCount;i++)
            {
                Transform child=group.GetChild(i);
                if(child.childCount!=0 || !PlainNode(child,allowDecal)) return false;
            }
            return NoColliderBeside(group);
        }
        // 물체의 모습과 콜라이더가 LOD 묶음 바깥에 나란히 있는 구조도 막습니다. 큰 폴더는 서로 무관한 물체입니다.
        private static bool NoColliderBeside(Transform group)
        {
            Transform outer=group.parent;
            if(outer==null || outer.childCount>32) return true;
            for(int i=0;i<outer.childCount;i++)
                if(outer.GetChild(i).GetComponent<Collider>()!=null) return false;
            return true;
        }
        // 데칼은 투영이라 몸체가 없으므로 형제 검사를 하지 않습니다. 맵 폴더에 수백 개가 나란히 있습니다.
        // 로딩 프레임 한 번에 수천 개가 들어오므로 같은 부모의 판정을 재사용합니다.
        internal bool LoadingDecalSafe(Transform decal)
        {
            if(!PlainNode(decal,true)) return false;
            Transform parent=decal.parent;
            if(parent==null) return true;
            int id=parent.GetInstanceID();
            bool safe;
            if(!loadingParents.TryGetValue(id,out safe)) { safe=ContainerChainSafe(parent); loadingParents.Add(id,safe); }
            return safe;
        }
        internal void Clear() { components.Clear(); loadingParents.Clear(); }
    }
}
