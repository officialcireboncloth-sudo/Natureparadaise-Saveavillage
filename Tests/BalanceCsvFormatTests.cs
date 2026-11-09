using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

static class BalanceCsvFormatTests
{
    static int checks;
    static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
    static void Invalid(string csv,string label)
    {
        var result=BalanceCsvFormat.Parse(csv,out _,out string error);
        Check(error!=null&&result.Count==0,label);
    }
    static void Main(string[] args)
    {
        var rows=BalanceCsvFormat.Parse("id,name,note\r\nitem.a,\"Kedelai, segar\",\"baris 1\r\nbaris 2 \"\"ya\"\"\"\r\n",out var headers,out var error);
        Check(error==null&&rows.Count==1&&headers.SequenceEqual(new[]{"id","name","note"}),"Comma header and one record");
        Check(rows[0]["name"]=="Kedelai, segar"&&rows[0]["note"]=="baris 1\r\nbaris 2 \"ya\"","Quoted commas, quotes, multiline text");
        rows=BalanceCsvFormat.Parse("\uFEFF\"id\",note\nx,\"\"",out _,out error);
        Check(error==null&&rows[0]["note"]=="","Quoted BOM header and empty final field");
        rows=BalanceCsvFormat.Parse("id,note\rx,\r",out _,out error);
        Check(error==null&&rows[0]["note"]=="","CR records and trailing empty field");
        rows=BalanceCsvFormat.Parse("id,note\n\nx,0.25\n\n",out _,out error);
        Check(error==null&&rows.Count==1&&rows[0]["note"]=="0.25","Blank records skipped without changing numbers");
        Invalid("id,ID\nx,y\n","Duplicate header rejected ignoring case");
        Invalid("id,\nx,y\n","Empty header rejected");
        Invalid("","Empty file rejected");
        Invalid("id,name\nx,\"unfinished","Unclosed quoted field rejected");
        Invalid("id,name\nx,un\"quoted\n","Quotes in unquoted field rejected");
        Invalid("id,name\nx,\"quoted\"extra\n","Characters after closing quote rejected");
        Invalid("id,price\nx,6,400\n","Unquoted thousands separator cannot shift columns");
        string temp=Path.Combine(Path.GetTempPath(),"NatureParadiseCsv-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try
        {
            string file=Path.Combine(temp,"sample.csv");
            var source=new[]{new[]{"item.a","Susu kambing",""},new[]{"item.b","Harga, 6400","dua\nbaris"}};
            BalanceCsvFormat.Write(file,new[]{"id","name","note"},source);
            byte[] bytes=File.ReadAllBytes(file);
            Check(bytes.Take(3).SequenceEqual(new byte[]{239,187,191}),"UTF-8 BOM for Excel");
            Check(Encoding.UTF8.GetString(bytes).Contains("id,name,note\r\n"),"CRLF records");
            rows=BalanceCsvFormat.Read(file,out headers,out error);
            Check(error==null&&rows.Select((r,i)=>headers.Select(h=>r[h]).SequenceEqual(source[i])).All(v=>v),"Writer roundtrip preserves text/empty cells");
            string before=File.ReadAllText(file);bool threw=false;
            try{BalanceCsvFormat.Write(file,new[]{"id","name"},new[]{new[]{"x"}});}catch(InvalidDataException){threw=true;}
            Check(threw&&File.ReadAllText(file)==before,"Invalid output cannot overwrite an existing file");
            BalanceCsvFormat.Read(Path.Combine(temp,"missing.csv"),out _,out error);Check(error!=null,"Missing file returns useful error");
            File.WriteAllBytes(file,new byte[]{(byte)'i',(byte)'d',10,0x96,10});
            BalanceCsvFormat.Read(file,out _,out error);Check(error!=null&&error.Contains("UTF-8"),"Invalid ANSI input requests UTF-8");
            int files=0;
            if(args.Length>0)
            {
                foreach(string original in Directory.GetFiles(args[0],"*.csv",SearchOption.AllDirectories))
                {
                    var originalRows=BalanceCsvFormat.Read(original,out var columns,out error);
                    if(error!=null)throw new Exception(original+": "+error);
                    BalanceCsvFormat.Write(file,columns,originalRows.Select(r=>columns.Select(h=>r[h]).ToArray()));
                    var after=BalanceCsvFormat.Read(file,out var afterHeaders,out error);
                    Check(error==null&&columns.SequenceEqual(afterHeaders)&&after.Count==originalRows.Count&&
                        after.Select((r,i)=>columns.All(h=>r[h]==originalRows[i][h])).All(v=>v),"Real data roundtrip "+Path.GetFileName(original));
                    files++;
                }
            }
            Console.WriteLine("PASS "+checks+" CSV format checks; "+files+" project CSV files roundtripped");
        }
        finally{foreach(string file in Directory.GetFiles(temp))File.Delete(file);Directory.Delete(temp);}
    }
}
