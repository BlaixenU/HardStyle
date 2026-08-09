using System.IO;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using TMPro;

namespace HardStyle;

public class Config
{
    internal static PluginConfigurator config = null!;

    private static string ModDir => Plugin.ModDir;

    // Hard Damage

    internal static FloatField hardMult;

    // Healing

    internal static FloatField[] healMults;

    internal static BoolField screwBypass;

    internal static FloatField screwMult;

    internal static BoolField roundingFix; // doesnt do anything rn

    // Style

    internal static BoolField whipStyleDecayAccel;

    internal static BoolField rateIncreaseEase;

    internal static FloatField startingDecayRate;

    internal static FloatField finalDecayRate;

    internal static FloatField easeTime;

    // Misc

    internal static BoolField malfaceFix;

    internal static BoolField scoreSubmission;

    internal static BoolField debugMode;


    public Config(string NAME, string GUID)
    {
        config = PluginConfigurator.Create(NAME, GUID);


        config.SetIconWithURL($"file://{Path.Combine(ModDir, "icon.png")}");

        // Hard Damage
        var hardDamageDivision = CreateDivision("Edit Hard Damage", "hardDamageEnabler", "hardDamageDivision");

        hardMult = new(hardDamageDivision, "Hard Damage Multiplier", "hardMult", 0.0f);

        // Healing
        var healingDivision = CreateDivision("Edit Healing", "healingEnabler", "healingDivision");

        ConfigHeader healMultHeader = new(healingDivision, "Healing Multipliers", 24);

        healMults[0] = new(healingDivision, "DESTRUCTIVE", "rank0", 0.1f);
        healMults[1] = new(healingDivision, "CHAOTIC", "rank1", 0.133333f);
        healMults[2] = new(healingDivision, "BRUTAL", "rank2", 0.166667f);
        healMults[3] = new(healingDivision, "ANARCHIC", "rank3", 0.200000f);
        healMults[4] = new(healingDivision, "SUPREME", "rank4", 0.233333f);
        healMults[5] = new(healingDivision, "SSADISTIC", "rank5", 0.266667f);
        healMults[6] = new(healingDivision, "SSSHITSTORM", "rank6", 0.300000f);
        healMults[7] = new(healingDivision, "ULTRAKILL", "rank7", 1f);

        ConfigSpace space = new(healingDivision, 5f);

        screwBypass = new(healingDivision, "Screwdriver Bypass", "screwBypass", true);

        ConfigDivision screwdriverDivision = new(healingDivision, "screwdriverDivision");
        screwBypass.onValueChange += e => screwdriverDivision.interactable = e.value;

        screwMult = new(screwdriverDivision, "Screw Multiplier", "screwMult", 0.5f);

        roundingFix = new(healingDivision, "Rounding Fix", "roundingFix", true);

        // Style
        var styleDivision = CreateDivision("Edit Style", "styleEnabler", "styleDivision");

        whipStyleDecayAccel = new(styleDivision, "Whiplash Style Reduction", "whipStyleDecayAccel", true);

        ConfigDivision whiplashDivision = new(styleDivision, "whiplashDivision");
        whipStyleDecayAccel.onValueChange += e => whiplashDivision.interactable = e.value;

        startingDecayRate = new(whiplashDivision, "Starting Decay Rate", "startingDecayRate", 1.5f);

        rateIncreaseEase = new(whiplashDivision, "Decay Rate Increase Ease", "rateIncreaseEase", true);

        ConfigDivision whiplashEaseDivision = new(whiplashDivision, "whiplashEaseDivision");
        rateIncreaseEase.onValueChange += e => whiplashEaseDivision.interactable = e.value;

        finalDecayRate = new(whiplashEaseDivision, "Final Decay Rate", "finalDecayRate", 2f);

        easeTime = new(whiplashEaseDivision, "Ease Time", "easeTime", 1f);


        // Misc
        var miscDivision = CreateDivision("Edit Misc", "miscEnabler", "miscDivision");

        malfaceFix = new(miscDivision, "Malface Bleed Fix", "malfaceFix", true);

        scoreSubmission = new(miscDivision, "Score Submission", "scoreSubmission", false);

        ConfigHeader troll = new(miscDivision, "Lol! This doesn't do anything!", 24);
        scoreSubmission.onValueChange += e => whiplashEaseDivision.hidden = !e.value;

        debugMode = new(miscDivision, "Debug Mode (Logging)", "debugMode", false);
    }

    private static ConfigDivision CreateDivision(string enablerDisplayName, string enablerGuid, string divisionGuid)
    {
        BoolField enabler = new(config.rootPanel, enablerDisplayName, enablerGuid, false);
        ConfigDivision division = new(config.rootPanel, divisionGuid);
        enabler.onValueChange += e => division.interactable = e.value;

        return division;
    }
}