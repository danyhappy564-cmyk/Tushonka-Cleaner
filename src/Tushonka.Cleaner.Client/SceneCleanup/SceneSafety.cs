// SPDX-License-Identifier: GPL-3.0-only
using System.Collections.Generic;
using UnityEngine;

namespace Tushonka.Cleaner.SceneCleanup
{
    // 허용 목록 밖의 컴포넌트는 모두 보호합니다. 특정 게임 타입 이름에 의존한 제외보다 보수적입니다.
    internal sealed class SceneSafety
    {
        private readonly List<Component> components=new List<Component>(128);
        internal bool PlainNode(Transform node, bool allowDecal)
        {
            node.GetComponents(components);
            bool safe=components.Count<=128;
            for(int i=0;i<components.Count && safe;i++)
            {
                Component c=components[i];
                safe=c!=null && (c is Transform || c is MeshFilter || c is MeshRenderer || c is LODGroup || (allowDecal && c is StaticDeferredDecal));
            }
            components.Clear();
            return safe;
        }
        internal bool AncestorsSafe(Transform node,bool allowDecal)
        {
            int depth=0;
            for(Transform t=node;t!=null;t=t.parent)
                if(++depth>64 || !PlainNode(t,allowDecal)) return false;
            return true;
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
            return true;
        }
        internal void Clear() { components.Clear(); }
    }
}
