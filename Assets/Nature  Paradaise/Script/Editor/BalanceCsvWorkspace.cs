using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Normalizes CSV packaging without changing headers, row order or cell values.</summary>
public static class BalanceCsvWorkspace
{
    public const string Root="Assets/Nature  Paradaise/Data/Balance";
    public static string LastReport {get;private set;}="";
    [MenuItem("Nature Paradise/Data CSV/Format/Standardize Existing CSV Files")]
    public static void Standardize()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Keluar Play Mode dahulu.");
        var prepared=new List<(string path,string[] headers,List<Dictionary<string,string>> rows)>();
        foreach(string file in Directory.GetFiles(Root,"*.csv",SearchOption.AllDirectories).OrderBy(f=>f,StringComparer.Ordinal))
        {
            var rows=BalanceCsvFormat.Read(file,out var headers,out string error);
            if(error!=null)throw new InvalidDataException(file+": "+error);
            prepared.Add((file,headers,rows));
        }
        string backup="BalanceBackups/CsvFormat-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        foreach(var file in prepared)
        {
            string destination=Path.Combine(backup,Path.GetRelativePath(Root,file.path));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file.path,destination);
        }
        bool previous=WeatherCsvDatabase.SuppressAutoImport;WeatherCsvDatabase.SuppressAutoImport=true;
        try
        {
            foreach(var file in prepared)BalanceCsvFormat.Write(file.path,file.headers,file.rows.Select(r=>file.headers.Select(h=>r[h]).ToArray()));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        finally{WeatherCsvDatabase.SuppressAutoImport=previous;}
        LastReport=prepared.Count+" file CSV dirapikan ke UTF-8 BOM, koma dan CRLF. Nilai sel/urutan tetap. Backup: "+backup;
        Debug.Log("[CSV FORMAT] "+LastReport);
    }
}
