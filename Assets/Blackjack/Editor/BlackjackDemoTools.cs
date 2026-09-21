using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blackjack;

public static class BlackjackDemoTools
{
    [MenuItem("Blackjack/Run Rule Checks")]
    public static void CheckRules()
    {
        int checks = 0;
        Action<bool,string> check = (ok, name) => { if (!ok) throw new Exception("Blackjack check failed: " + name); checks++; };
        check(BlackjackRound.Total(new[] {new Card(1),new Card(1),new Card(9)}) == 21,"multiple aces");
        check(BlackjackRound.Total(new[] {new Card(1),new Card(9),new Card(5)}) == 15,"soft to hard");
        var r = new BlackjackRound(1);
        check(!r.Deal(0) && !r.Deal(1001) && !r.Deal(15),"invalid bets");
        r.SetTestShoe(new Card(1),new Card(9),new Card(13),new Card(8)); r.Deal(100);
        check(r.Bank == 1150 && r.State == BlackjackRound.Phase.Complete,"natural pays 3:2");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(1),new Card(1),new Card(10),new Card(13)); r.Deal(100);
        check(r.Bank == 1000,"natural push");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(9),new Card(1),new Card(10),new Card(13)); r.Deal(100);
        check(r.Bank == 900,"dealer natural");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(5),new Card(10),new Card(6),new Card(7),new Card(10)); r.Deal(100); r.Double();
        check(r.Bank == 800 && r.Bet == 200 && r.Player.Count == 3 && r.State == BlackjackRound.Phase.Dealer,"double deduction and forced stand");
        r.DealerStep(); check(r.Bank == 1200,"double win");
        int bank = r.Bank; r.DealerStep(); r.Double(); r.Hit(); check(r.Bank == bank,"settlement cannot repeat");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(10),new Card(10),new Card(6),new Card(7),new Card(10)); r.Deal(100); r.Hit();
        check(r.State == BlackjackRound.Phase.Complete && r.Bank == 900,"bust");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(10),new Card(1),new Card(8),new Card(6)); r.Deal(100); r.Stand(); r.DealerStep();
        check(r.Dealer.Count == 2 && r.Bank == 1100,"dealer stands on soft 17");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(10),new Card(10),new Card(8),new Card(6),new Card(10)); r.Deal(100); r.Stand(); r.DealerStep();
        check(r.Bank == 1100,"dealer bust");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(10),new Card(10),new Card(8),new Card(8)); r.Deal(100); r.Stand(); r.DealerStep();
        check(r.Bank == 1000,"ordinary push");
        r = new BlackjackRound(1); r.SetTestShoe(new Card(5),new Card(10),new Card(6),new Card(7)); r.Deal(1000); check(!r.CanDouble,"insufficient bank for double");
        for (int i=0; i<500; i++)
        {
            r = new BlackjackRound(i); r.Deal(50);
            while (r.State == BlackjackRound.Phase.Player && BlackjackRound.Total(r.Player) < 17) r.Hit();
            r.Stand(); int steps = 0;
            while (r.State == BlackjackRound.Phase.Dealer && steps++ < 30) r.DealerStep();
            check(r.State == BlackjackRound.Phase.Complete && r.Bank >= 0,"simulated round " + i);
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/BlackjackRuleChecks.txt", "PASS: " + checks + " checks, including 500 simulated rounds. " + DateTime.Now);
        Debug.Log("Blackjack: " + checks + " rule checks passed.");
    }

    [MenuItem("Blackjack/Build Windows Demo")]
    public static void BuildDemo()
    {
        CheckRules();
        PlayerSettings.productName = "Blackjack Demo";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] {"Assets/Scenes/SampleScene.unity"},
            locationPathName = "Builds/Blackjack/Blackjack.exe", target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Blackjack build failed: " + report.summary.result);
    }
}
