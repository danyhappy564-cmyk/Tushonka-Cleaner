using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// 설치된 참조 DLL만 이용하는 읽기 전용 형식 검사입니다. DLL을 생성하거나 게임을 실행하지 않습니다.
internal static class StaticTypeCheck
{
    internal static int Run(string root)
    {
        var dotnet = Path.GetDirectoryName(Environment.ProcessPath)!;
        var game = Environment.GetEnvironmentVariable("SPT_ROOT") ?? @"E:\SPT 4.1";
        var errors = 0;
        foreach (var client in new[] { true, false })
        {
            var refs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void AddReferences(string directory)
            {
                foreach (var path in Directory.EnumerateFiles(directory, "*.dll"))
                    refs.TryAdd(Path.GetFileName(path), path);
            }
            string Pack(string name, string framework) => Path.Combine(
                Directory.GetDirectories(Path.Combine(dotnet, "packs", name)).OrderBy(p => Version.Parse(Path.GetFileName(p))).Last(),
                "ref", framework);
            AddReferences(client ? Pack("NETStandard.Library.Ref", "netstandard2.1") : Pack("Microsoft.NETCore.App.Ref", "net10.0"));
            if (client)
            {
                foreach (var name in new[] { "BepInEx", "0Harmony" })
                    refs[name + ".dll"] = Path.Combine(game, "BepInEx", "core", name + ".dll");
                foreach (var name in new[] { "Assembly-CSharp", "Comfort", "UnityEngine", "UnityEngine.CoreModule", "UnityEngine.IMGUIModule",
                             "UnityEngine.TextRenderingModule", "UnityEngine.PhysicsModule", "UnityEngine.VideoModule", "Newtonsoft.Json" })
                    refs[name + ".dll"] = Path.Combine(game, "EscapeFromTarkov_Data", "Managed", name + ".dll");
            }
            else
            {
                AddReferences(Pack("Microsoft.AspNetCore.App.Ref", "net10.0"));
                AddReferences(Path.Combine(game, "SPT_Runtime"));
            }
            var metadata = new List<MetadataReference>();
            foreach (var path in refs.Values)
            {
                try
                {
                    using var stream = File.OpenRead(path);
                    using var pe = new PEReader(stream);
                    if (pe.HasMetadata) metadata.Add(MetadataReference.CreateFromFile(path));
                }
                catch (BadImageFormatException) { /* 네이티브 라이브러리는 C# 참조가 아닙니다. */ }
            }
            foreach (var bench in new[] { false, true })
            {
                var symbols = new List<string> { client ? "NETSTANDARD2_1" : "NET10_0" };
                if (bench) symbols.Add("BENCH");
                var parse = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: symbols);
                var sourceDir = Path.Combine(root, "src", client ? "Tushonka.Cleaner.Client" : "Tushonka.Cleaner.Server");
                var paths = Directory.EnumerateFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                    .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                                !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                    .Concat(Directory.EnumerateFiles(Path.Combine(root, "src", "Shared"), "*.cs"));
                var trees = paths.Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), parse, p)).ToList();
                var imports = "System;System.Collections.Generic;System.IO;System.Linq;System.Net.Http;System.Threading;System.Threading.Tasks";
                if (!client) imports += ";System.Net.Http.Json;Microsoft.AspNetCore.Builder;Microsoft.AspNetCore.Hosting;Microsoft.AspNetCore.Http;Microsoft.AspNetCore.Routing;Microsoft.Extensions.Configuration;Microsoft.Extensions.DependencyInjection;Microsoft.Extensions.Hosting;Microsoft.Extensions.Logging";
                trees.Add(CSharpSyntaxTree.ParseText(string.Join("\n", imports.Split(';').Select(s => "global using global::" + s + ";")), parse));
                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: client ? NullableContextOptions.Disable : NullableContextOptions.Enable,
                    optimizationLevel: OptimizationLevel.Release,
                    // dotnet msbuild -getProperty:NoWarn로 확인한 두 프로젝트의 SDK 기본값입니다.
                    specificDiagnosticOptions: new Dictionary<string, ReportDiagnostic>
                    {
                        ["CS1701"] = ReportDiagnostic.Suppress,
                        ["CS1702"] = ReportDiagnostic.Suppress,
                    });
                var compilation = CSharpCompilation.Create(client ? "Tushonka.Cleaner" : "Tushonka.Cleaner.Server", trees, metadata, options);
                var diagnostics = compilation.GetDiagnostics();
                var failures = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                var warnings = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
                foreach (var diagnostic in failures) Console.WriteLine(diagnostic);
                foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning)) Console.WriteLine(diagnostic);
                Console.WriteLine($"읽기 전용 형식 검사: {(client ? "클라이언트" : "서버")} {(bench ? "BENCH" : "Release")}, 오류 {failures.Length}개, 경고 {warnings}개. DLL 미생성.");
                errors += failures.Length;
            }
        }
        return errors;
    }
}
