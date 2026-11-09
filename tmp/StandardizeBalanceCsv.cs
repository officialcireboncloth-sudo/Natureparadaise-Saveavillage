using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

static class StandardizeBalanceCsv
{
    static void Main()
    {
        string root="Assets/Nature  Paradaise/Data/Balance";
        string backup="BalanceBackups/CsvFormat-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        var ready=new List<(string file,string[] headers,List<Dictionary<string,string>> rows)>();
        foreach(var file in Directory.GetFiles(root,"*.csv",SearchOption.AllDirectories).OrderBy(p=>p))
        {
            var rows=BalanceCsvFormat.Read(file,out var headers,out string error);
            if(error!=null)throw new Exception(file+": "+error);
            ready.Add((file,headers,rows));
        }
        foreach(var item in ready)
        {
            string relative=item.file.Substring(root.Length).TrimStart('/','\\');
            string target=Path.Combine(backup,relative);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(item.file,target);
        }
        foreach(var item in ready)BalanceCsvFormat.Write(item.file,item.headers,item.rows.Select(r=>item.headers.Select(h=>r[h]).ToArray()));
        Console.WriteLine("Normalized "+ready.Count+" CSV files; backup "+backup);
    }
}
