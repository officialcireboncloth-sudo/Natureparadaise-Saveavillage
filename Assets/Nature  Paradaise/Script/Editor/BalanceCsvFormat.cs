#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Shared CSV contract: UTF-8 BOM, comma separator, CRLF records and quoted multiline fields.</summary>
public static class BalanceCsvFormat
{
    static void CheckHeaders(string[] headers)
    {
        if(headers==null||headers.Length==0||headers.Any(string.IsNullOrWhiteSpace))throw new InvalidDataException("Header CSV tidak boleh kosong.");
        if(headers.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=headers.Length)throw new InvalidDataException("Nama kolom CSV duplikat.");
    }
    public static void Write(string path,string[] headers,IEnumerable<string[]> rows)
    {
        CheckHeaders(headers);
        var text=new StringBuilder();text.Append(string.Join(",",headers.Select(Escape))).Append("\r\n");
        int line=1;
        foreach(var row in rows)
        {
            line++;if(row==null||row.Length!=headers.Length)throw new InvalidDataException("Baris "+line+": jumlah kolom tidak sama dengan header.");
            text.Append(string.Join(",",row.Select(Escape))).Append("\r\n");
        }
        string directory=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
        File.WriteAllText(path,text.ToString(),new UTF8Encoding(true));
    }
    public static List<Dictionary<string,string>> Read(string path,out string[] headers,out string error)
    {
        headers=Array.Empty<string>();error=null;
        try
        {
            string text;
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
            using(var reader=new StreamReader(stream,new UTF8Encoding(false,true),true))text=reader.ReadToEnd();
            return Parse(text,out headers,out error);
        }
        catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException||ex is DecoderFallbackException)
        {error="CSV tidak dapat dibaca. Simpan sebagai CSV UTF-8 dengan pemisah koma. "+ex.Message;return new List<Dictionary<string,string>>();}
    }
    public static List<Dictionary<string,string>> Parse(string text,out string[] headers,out string error)
    {
        headers=Array.Empty<string>();error=null;text=(text??string.Empty).TrimStart('\uFEFF');var records=new List<List<string>>();
        var row=new List<string>();var field=new StringBuilder();bool quoted=false,closedQuote=false;
        for(int i=0;i<text.Length;i++)
        {
            char c=text[i];
            if(quoted)
            {
                if(c=='"'&&i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}
                else if(c=='"'){quoted=false;closedQuote=true;}
                else field.Append(c);
                continue;
            }
            if(c==','){row.Add(field.ToString());field.Clear();closedQuote=false;}
            else if(c=='\r'||c=='\n')
            {
                if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;
                row.Add(field.ToString());field.Clear();closedQuote=false;
                if(row.Any(v=>!string.IsNullOrWhiteSpace(v)))records.Add(row);row=new List<string>();
            }
            else if(closedQuote)
            {if(c!=' '&&c!='\t'){error="Karakter tidak valid setelah penutup tanda kutip CSV.";return new List<Dictionary<string,string>>();}}
            else if(c=='"')
            {if(field.Length!=0){error="Tanda kutip di tengah field harus di-escape sebagai dua tanda kutip.";return new List<Dictionary<string,string>>();}quoted=true;}
            else field.Append(c);
        }
        if(quoted){error="Tanda kutip CSV tidak ditutup.";return new List<Dictionary<string,string>>();}
        if(field.Length>0||row.Count>0||closedQuote){row.Add(field.ToString());if(row.Any(v=>!string.IsNullOrWhiteSpace(v)))records.Add(row);}
        if(records.Count==0){error="File CSV kosong.";return new List<Dictionary<string,string>>();}
        headers=records[0].Select(v=>v.Trim().TrimStart('\uFEFF')).ToArray();
        try{CheckHeaders(headers);}catch(InvalidDataException ex){error=ex.Message;return new List<Dictionary<string,string>>();}
        var result=new List<Dictionary<string,string>>();
        for(int i=1;i<records.Count;i++)
        {
            if(records[i].Count!=headers.Length){error=$"Baris {i+1} memiliki {records[i].Count} kolom; seharusnya {headers.Length}. Gunakan pemisah koma.";return new List<Dictionary<string,string>>();}
            var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            for(int c=0;c<headers.Length;c++)values.Add(headers[c],records[i][c]);result.Add(values);
        }
        return result;
    }
    static string Escape(string value)
    {
        value=value??string.Empty;
        return value.IndexOfAny(new[]{',','"','\r','\n'})>=0?'"'+value.Replace("\"","\"\"")+'"':value;
    }
}
#endif
