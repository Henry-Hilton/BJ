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
        var profile = new BlackjackPlayerProfile { displayName = "  Jane Doe  ", colorA = "#123456", colorB = "ABCDEF", avatarStyle = AvatarStyle.Gradient };
        profile.Normalize(); var profileCopy = profile.Copy();
        check(profile.displayName == "Jane Doe" && profile.Initials == "JD", "profile name and initials");
        check(profile.colorA == "123456" && profile.colorB == "ABCDEF", "profile hex normalization");
        check(profileCopy.profileId == profile.profileId && profileCopy.avatarStyle == AvatarStyle.Gradient && profileCopy.colorB == profile.colorB, "profile JSON roundtrip retains identity and avatar");
        profileCopy.colorA = "FFFFFF"; check(profile.colorA == "123456", "profile copy is isolated");
        check(!BlackjackPlayerProfile.TryColor("#12", out _) && !BlackjackPlayerProfile.TryColor("ZZZZZZ", out _), "invalid avatar colors rejected");
        profileCopy.profileId = "invalid"; profileCopy.colorA = "bad"; profileCopy.avatarStyle = (AvatarStyle)99; profileCopy.Normalize();
        check(Guid.TryParseExact(profileCopy.profileId, "N", out _) && profileCopy.avatarStyle == AvatarStyle.Solid && profileCopy.colorA.Length == 6, "profile invalid data repaired");
        profileCopy.displayName = "<b>name</b>"; profileCopy.Normalize(); check(profileCopy.displayName == "", "profile markup rejected");
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
        check(!BlackjackDealerAI.ShouldHit(DealerDifficulty.Easy, new[] {new Card(10),new Card(5)}, 20), "easy stands on 15");
        check(BlackjackDealerAI.ShouldHit(DealerDifficulty.Easy, new[] {new Card(10),new Card(4)}, 20), "easy hits 14");
        check(BlackjackDealerAI.ShouldHit(DealerDifficulty.Medium, new[] {new Card(10),new Card(6)}, 20), "medium hits 16");
        check(!BlackjackDealerAI.ShouldHit(DealerDifficulty.Hard, new[] {new Card(10),new Card(4)}, 13), "hard protects a winning hand");
        check(BlackjackDealerAI.ShouldHit(DealerDifficulty.Hard, new[] {new Card(10),new Card(9)}, 20), "hard draws when standing loses");
        check(!BlackjackDealerAI.ShouldHit(DealerDifficulty.Hard, new[] {new Card(10),new Card(10)}, 20), "hard keeps a strong tie");
        check(BlackjackDealerAI.ShouldHit(DealerDifficulty.Hard, new[] {new Card(1),new Card(6)}, 20), "hard evaluates soft hands");
        r = new BlackjackRound(1); r.SetDifficulty(DealerDifficulty.Easy);
        r.SetTestShoe(new Card(10),new Card(10),new Card(8),new Card(5)); r.Deal(100);
        check(!r.SetDifficulty(DealerDifficulty.Hard) && r.Difficulty == DealerDifficulty.Easy,"difficulty locked during player turn");
        r.Stand(); check(!r.SetDifficulty(DealerDifficulty.Hard),"difficulty locked during dealer turn");
        r.DealerStep(); check(r.State == BlackjackRound.Phase.Complete && r.Bank == 1100 && r.Dealer.Count == 2,"easy rule integrated");
        check(r.SetDifficulty(DealerDifficulty.Hard),"difficulty editable after settlement");
        check(!r.SetDifficulty((DealerDifficulty)99),"invalid difficulty rejected");
        foreach (DealerDifficulty difficulty in Enum.GetValues(typeof(DealerDifficulty)))
        for (int i=0; i<500; i++)
        {
            r = new BlackjackRound(i); r.SetDifficulty(difficulty); r.Deal(50);
            while (r.State == BlackjackRound.Phase.Player && BlackjackRound.Total(r.Player) < 17) r.Hit();
            r.Stand(); int steps = 0;
            while (r.State == BlackjackRound.Phase.Dealer && steps++ < 30) r.DealerStep();
            check(r.State == BlackjackRound.Phase.Complete && r.Bank >= 0,"simulated " + difficulty + " round " + i);
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/BlackjackRuleChecks.txt", "PASS: " + checks + " checks, including 1,500 simulated rounds across three difficulties. " + DateTime.Now);
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
