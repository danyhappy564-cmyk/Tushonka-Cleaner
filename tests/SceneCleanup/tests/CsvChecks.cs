using System;
using System.IO;
using System.Text.Json;
using System.Globalization;
using System.Collections.Generic;
using Tushonka.Cleaner.SceneCleanup;

internal static class CsvChecks
{
    public static void Run(string root,RuleBook rules)
    {
        var actual=new Dictionary<string,int[]>();
        int rows=0;
        foreach(string path in Directory.GetFiles(Path.Combine(root,"scene-names"),"names-*.csv"))
        {
            string map=Path.GetFileNameWithoutExtension(path).Substring(6);
            var counts=new int[7]; actual.Add(map,counts);
            bool header=true;
            foreach(string line in File.ReadLines(path))
            {
                if(header){header=false;continue;}
                if(line.Length==0)continue;
                string[] fields=Parse(line);
                var category=rules.Classify(fields[0],fields[1],float.Parse(fields[3],CultureInfo.InvariantCulture),float.Parse(fields[4],CultureInfo.InvariantCulture)>0);
                if(category!=CleanupCategory.None)counts[(int)category]+=int.Parse(fields[2],CultureInfo.InvariantCulture);
                rows++;
            }
        }
        using(var expected=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"data/rule-audit.json"))))
        {
            foreach(var map in expected.RootElement.GetProperty("counts").EnumerateObject())
                for(int category=0;category<7;category++)
                {
                    int target=map.Value.GetProperty(((CleanupCategory)category).ToString()).GetInt32();
                    if(actual[map.Name][category]!=target)throw new Exception("CSV parity "+map.Name+"/"+(CleanupCategory)category+": "+actual[map.Name][category]+" != "+target);
                }
        }
        Console.WriteLine("PASS CSV "+rows+" rows / "+actual.Count+" maps / 70 category counts");
    }
    private static string[] Parse(string line)
    {
        var fields=new List<string>();
        var field=new System.Text.StringBuilder();bool quoted=false;
        for(int i=0;i<line.Length;i++)
        {
            char c=line[i];
            if(c=='\"')
            {
                if(quoted && i+1<line.Length && line[i+1]=='\"'){field.Append(c);i++;}else quoted=!quoted;
            }
            else if(c==',' && !quoted){fields.Add(field.ToString());field.Clear();}
            else field.Append(c);
        }
        fields.Add(field.ToString());return fields.ToArray();
    }
}
