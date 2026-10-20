#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-shot, explicitly requested editor save when the editor window cannot be drawn.</summary>
[InitializeOnLoad]
public static class EditorRecoverySave
{
    const string Request="Temp/EditorRecoverySave.request";
    static bool busy;
    static EditorRecoverySave(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        busy=true;
        string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        string folder="Library/EditorSaveRecovery/"+stamp;
        var log=new StringBuilder("EXPLICIT USER-REQUESTED EDITOR SAVE\n");
        try
        {
            Directory.CreateDirectory(folder);
            File.Move(Request,folder+"/Request.txt");
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Save refused during Play Mode; runtime state must not overwrite authored scenes.");
            int saved=0,failed=0,unnamed=0;
            log.AppendLine("Active scene: "+SceneManager.GetActiveScene().path);
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                if(!scene.isLoaded||EditorSceneManager.IsPreviewScene(scene))continue;
                log.AppendLine($"SCENE {scene.path} dirtyBefore={scene.isDirty} roots={scene.rootCount}");
                if(!scene.isDirty){log.AppendLine("UNCHANGED — already saved");continue;}
                if(string.IsNullOrEmpty(scene.path))
                {
                    unnamed++;log.AppendLine("NOT SAVED — untitled scene requires a user-selected path");continue;
                }
                string full=Path.GetFullPath(scene.path),assets=Path.GetFullPath("Assets")+Path.DirectorySeparatorChar;
                if(!full.StartsWith(assets,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing unexpected scene path: "+scene.path);
                string backup=folder+"/BeforeSave/"+scene.path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if(File.Exists(scene.path))File.Copy(scene.path,backup,false);
                bool ok=EditorSceneManager.SaveScene(scene);
                if(ok&&!scene.isDirty&&File.Exists(scene.path))
                {
                    saved++;log.AppendLine("SAVED "+scene.path+" bytes="+new FileInfo(scene.path).Length+" dirtyAfter=False");
                }
                else{failed++;log.AppendLine("FAILED "+scene.path+" dirtyAfter="+scene.isDirty);}
            }
            AssetDatabase.SaveAssets();
            log.AppendLine($"RESULT saved={saved} failed={failed} unnamed={unnamed}; assets saved; Unity was NOT closed.");
            log.AppendLine(failed==0&&unnamed==0?"SAVE_VERIFIED":"SAVE_INCOMPLETE");
            Debug.Log("[EDITOR RECOVERY SAVE] "+log);
        }
        catch(Exception e){log.AppendLine("SAVE_FAILED "+e);Debug.LogException(e);}
        finally
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder+"/SaveReport.txt",log.ToString());
            File.WriteAllText("Library/EditorSaveRecovery/LatestReport.txt",log.ToString());
            busy=false;
        }
    }
}
#endif
