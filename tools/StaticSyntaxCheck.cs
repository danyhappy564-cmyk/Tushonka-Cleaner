using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// NuGet 복원과 독립된 구문 검사입니다. 형식 검사·빌드·테스트 결과를 대신하지 않습니다.
internal static class StaticSyntaxCheck
{
    private static int Main(string[] folders)
    {
        var errors = 0;
        var count = 0;
        foreach (var folder in folders)
        foreach (var path in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
            count++;
            foreach (var symbols in new[] { Array.Empty<string>(), new[] { "BENCH", "NETSTANDARD2_1" } })
            {
                var options = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: symbols);
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path);
                foreach (var diagnostic in tree.GetDiagnostics())
                {
                    if (diagnostic.Severity != DiagnosticSeverity.Error) continue;
                    Console.WriteLine(diagnostic);
                    errors++;
                }
            }
        }
        Console.WriteLine($"C# 구문 검사: {count}개 파일, Release·BENCH 구문 오류 {errors}개. 형식 검사·빌드·테스트는 별도입니다.");
        if (errors == 0) errors += StaticTypeCheck.Run(Directory.GetCurrentDirectory());
        return errors == 0 ? 0 : 1;
    }
}
