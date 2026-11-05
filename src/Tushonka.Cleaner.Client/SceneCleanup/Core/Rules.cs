// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Tushonka.Cleaner.SceneCleanup
{
    public enum SceneCleanupLevel { Off, LightTouch, Balanced, Max }
    public enum CleanupCategory { None = -1, Litter, Rubble, Casings, Food, Puddles, Glass, Decals }
    internal enum ContainerKind { None, Inert, GroupRoot }

    // 조상에 있어도 게임 진행과 무관함을 게임 코드로 확인한 컴포넌트입니다. 처음 보는 타입은 None(보호)입니다.
    // - Koenigz.PerfectCulling.*: 렌더러 가시성만 제어합니다. MultisceneSharedOccluder는 설정값만 가집니다.
    // - GuidComponent: 고유 번호만 가집니다. LocationScene: 씬별 물체 목록을 등록합니다.
    // 그룹 루트(GroupRoot)는 씬 단위로 가림 묶음을 소유하므로 조상 검사의 끝으로 씁니다.
    internal static class ContainerRules
    {
        private const string GroupRootType="Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneGroup";
        public static ContainerKind Kind(string fullName)
        {
            if(fullName==null) return ContainerKind.None;
            if(fullName==GroupRootType) return ContainerKind.GroupRoot;
            if(fullName=="GuidComponent" || fullName=="LocationScene" || fullName.StartsWith("Koenigz.PerfectCulling.",StringComparison.Ordinal)) return ContainerKind.Inert;
            return ContainerKind.None;
        }
    }

    // 역추적이 한 번뿐인 선형 와일드카드 판정입니다. 정규식·문자열 복사·할당을 하지 않습니다.
    internal static class Glob
    {
        public static bool Match(string pattern, string text)
        { return text != null && Match(pattern, text, text.Length); }
        internal static bool Match(string pattern, string text, int length)
        {
            if (pattern == null || text == null) return false;
            int p=0, t=0, star=-1, retry=0;
            while (t < length)
            {
                if (p < pattern.Length && (pattern[p]=='?' || char.ToUpperInvariant(pattern[p])==char.ToUpperInvariant(text[t]))) { ++p; ++t; }
                else if (p < pattern.Length && pattern[p]=='*') { star=p++; retry=t; }
                else if (star>=0) { p=star+1; t=++retry; }
                else return false;
            }
            while (p<pattern.Length && pattern[p]=='*') ++p;
            return p==pattern.Length;
        }
    }
    internal sealed class ExclusionSet
    {
        private readonly string[] patterns;
        public ExclusionSet(string[] lines)
        {
            patterns = new string[lines.Length];
            for(int i=0;i<lines.Length;i++) { string s=lines[i].Trim(); patterns[i]=s.Length==0 || s[0]=='#' ? null:s; }
        }
        public bool Matches(string name)
        {
            if(name==null) return false;
            int normalized=RuleBook.NormalizedLength(name);
            for(int i=0;i<patterns.Length;i++) if(patterns[i]!=null && (Glob.Match(patterns[i],name) || Glob.Match(patterns[i],name,normalized))) return true;
            return false;
        }
    }
    [DataContract] internal sealed class RuleDocument
    {
        [DataMember(Name="version")] public int Version;
        [DataMember(Name="rules")] public RuleEntry[] Rules;
    }
    [DataContract] internal sealed class RuleEntry
    {
        [DataMember(Name="category")] public string Category;
        [DataMember(Name="pattern")] public string Pattern;
        [DataMember(Name="renderer")] public string Renderer;
        [DataMember(Name="maxSize")] public float MaxSize;
        public CleanupCategory Kind;
    }
    internal sealed class RuleBook
    {
        private readonly RuleEntry[] entries;
        // 데칼 상자 한 변의 상한(m)입니다. 등록 훅의 데칼은 이름 자료가 없어 타입과 이 값으로 판정합니다.
        public float DecalSizeLimit { get; private set; }
        private RuleBook(RuleEntry[] entries)
        {
            this.entries=entries;
            for(int i=0;i<entries.Length;i++) if(entries[i].Kind==CleanupCategory.Decals && entries[i].MaxSize>DecalSizeLimit) DecalSizeLimit=entries[i].MaxSize;
        }
        public static RuleBook Load(string json)
        {
            RuleDocument doc;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))
                doc=(RuleDocument)new DataContractJsonSerializer(typeof(RuleDocument)).ReadObject(stream);
            if(doc==null || doc.Version!=1 || doc.Rules==null || doc.Rules.Length>4096) throw new InvalidDataException("Invalid scene rules");
            for(int i=0;i<doc.Rules.Length;i++)
            {
                var r=doc.Rules[i]; CleanupCategory category;
                if(r==null || !Enum.TryParse(r.Category,out category) || (int)category<0 || (int)category>6 || string.IsNullOrEmpty(r.Pattern) || r.Pattern.Length>256 || r.Renderer!="MeshRenderer" || !(r.MaxSize>0) || float.IsInfinity(r.MaxSize))
                    throw new InvalidDataException("Invalid scene rule entry");
                r.Kind=category;
            }
            return new RuleBook(doc.Rules);
        }
        // 크기를 읽기 전에 이름만 봅니다. 맵의 렌더러 대부분은 여기서 끝나 네이티브 크기 조회를 하지 않습니다.
        public bool MatchesName(string name)
        {
            if(name==null) return false;
            int length=NormalizedLength(name);
            for(int i=0;i<entries.Length;i++) if(Glob.Match(entries[i].Pattern,name,length)) return true;
            return false;
        }
        public CleanupCategory Classify(string name,string renderer,float size,bool unsafeObject)
        {
            if(unsafeObject || name==null || !(size>0) || float.IsInfinity(size)) return CleanupCategory.None;
            int length=NormalizedLength(name);
            for(int i=0;i<entries.Length;i++)
            {
                var rule=entries[i];
                if(size<=rule.MaxSize && renderer==rule.Renderer && Glob.Match(rule.Pattern,name,length)) return rule.Kind;
            }
            return CleanupCategory.None;
        }
        // CSV는 LOD 번호·복제 번호를 정규화한 이름입니다. 원문을 잘라 새 문자열을 만들지 않습니다.
        internal static int NormalizedLength(string name)
        {
            int n=name.Length;
            if(n>=7 && string.Compare(name,n-7,"(Clone)",0,7,StringComparison.OrdinalIgnoreCase)==0) n-=7;
            while(n>0 && name[n-1]==' ') --n;
            int digits=n;
            while(digits>0 && name[digits-1]>='0' && name[digits-1]<='9') --digits;
            if(digits>=3 && string.Compare(name,digits-3,"LOD",0,3,StringComparison.OrdinalIgnoreCase)==0) n=digits;
            return n;
        }
    }
}
