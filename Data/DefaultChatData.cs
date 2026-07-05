using System.Collections.Generic;

namespace ChatUtilities.Data
{
    public class DefaultChatData
    {
        public static List<ChatCommandDefinition> CreateCommands()
        {
            return new List<ChatCommandDefinition>
            {
                new ChatCommandDefinition("/skip", "Skip to the next selected level in playlist"),
                new ChatCommandDefinition("/fs", "Alias for /skip"),
                new ChatCommandDefinition("/skiplevel", "Alias for /skip"),
                new ChatCommandDefinition("/skip random", "Skip to a random level"),
                new ChatCommandDefinition("/skip next", "Skip to the exact next level in playlist"),
                new ChatCommandDefinition("/skip prev", "Skip to the previous level in playlist"),
                new ChatCommandDefinition("/skip restart", "Restarts current level. Alternatively: /restart"),
                new ChatCommandDefinition("/skip [integer]", "Skip to specific level in playlist"),
                new ChatCommandDefinition("/gs", "Alias for /gamesettings"),
                new ChatCommandDefinition("/gamesettings", "Opens game settings"),
                new ChatCommandDefinition("/gs photomode on", "Enables photomode"),
                new ChatCommandDefinition("/gs photomode off", "Disables photomode"),
                new ChatCommandDefinition("/gs photomode timed [seconds]", "Enables photomode after X seconds from start of level"),
                new ChatCommandDefinition("/gs photomode enabledfinish", "Enables photomode for players who finished at least once"),
                new ChatCommandDefinition("/resettime", "Resets round time"),
                new ChatCommandDefinition("/settime [seconds]", "Sets round time to X seconds"),
                new ChatCommandDefinition("/joinmessage", "Sets and enables join message"),
                new ChatCommandDefinition("/joinmessage on", "Enables join message"),
                new ChatCommandDefinition("/joinmessage off", "Disables join message"),
                new ChatCommandDefinition("/joinmessage test", "Tests the join message, shows it to you alone"),
                new ChatCommandDefinition("/servermessage", "Sets and enables server message, will show for X seconds"),
                new ChatCommandDefinition("/servermessage remove", "Removes server message"),
                new ChatCommandDefinition("/vs", "Alias for /voteskip"),
                new ChatCommandDefinition("/voteskip", "Alias for /vs"),
                new ChatCommandDefinition("/vs on", "Enables voteskip system"),
                new ChatCommandDefinition("/vs off", "Disables voteskip system"),
                new ChatCommandDefinition("/vs reset", "Resets vote count"),
                new ChatCommandDefinition("/vs % [percentage]", "Sets vote threshold to percentage")
            };
        }

        public static List<EmoteDefinition> CreateEmotes()
        {
            return new List<EmoteDefinition>
            {
                new EmoteDefinition(":YannicS:", "<sprite=\"Zeepkist\" name=\"YannicScared\">"),
                new EmoteDefinition(":YannicSmile:", "<sprite=\"Zeepkist\" name=\"YannicSmile\">"),
                new EmoteDefinition(":YannicMegaS:", "<sprite=\"Zeepkist\" name=\"YannicEyes\">"),
                new EmoteDefinition(":eyes:", "<sprite=\"Zeepkist\" name=\"Eyes\">"),
                new EmoteDefinition(":smile:", "<sprite=\"Zeepkist\" name=\"Smile\">"),
                new EmoteDefinition(":skull:", "<sprite=\"Zeepkist\" name=\"Skull\">"),
                new EmoteDefinition(":amaze:", "<sprite=\"Zeepkist\" name=\"Amaze\">"),
                new EmoteDefinition(":good:", "<sprite=\"Zeepkist\" name=\"Good\">"),
                new EmoteDefinition(":sparkle:", "<sprite=\"Zeepkist\" name=\"Sparkle\">"),
                new EmoteDefinition(":heart:", "<sprite=\"Zeepkist\" name=\"Heart\">"),
                new EmoteDefinition(":love:", "<sprite=\"Zeepkist\" name=\"Love\">"),
                new EmoteDefinition(":cry:", "<sprite=\"Zeepkist\" name=\"Cry\">"),
                new EmoteDefinition(":party:", "<sprite=\"Zeepkist\" name=\"Party\">"),
                new EmoteDefinition(":CoolOrange:", "<sprite=\"moremojis\" name=\"CoolOrange\">"),
                new EmoteDefinition(":WhereMoney:", "<sprite=\"moremojis\" name=\"WhereMoney\">"),
                new EmoteDefinition(":RainbowMoney:", "<sprite=\"moremojis\" name=\"RainbowMoney\">"),
                new EmoteDefinition(":Money:", "<sprite=\"moremojis\" name=\"Money\">"),
                new EmoteDefinition(":ZaagBladPadRood2:", "<sprite=\"moremojis\" name=\"ZaagBladPadRood2\">"),
                new EmoteDefinition(":ZaagBladPadRood:", "<sprite=\"moremojis\" name=\"ZaagBladPadRood\">"),
                new EmoteDefinition(":ZaagBladPad2:", "<sprite=\"moremojis\" name=\"ZaagBladPad2\">"),
                new EmoteDefinition(":ZaagBladPad:", "<sprite=\"moremojis\" name=\"ZaagBladPad\">"),
                new EmoteDefinition(":Work:", "<sprite=\"moremojis\" name=\"Work\">"),
                new EmoteDefinition(":Euro:", "<sprite=\"moremojis\" name=\"Euro\">"),
                new EmoteDefinition(":!!:", "<sprite=\"moremojis\" name=\"Exclemation\">"),
                new EmoteDefinition(":<3:", "<sprite=\"moremojis\" name=\"Love\">"),
                new EmoteDefinition(":Z_:", "<sprite=\"moremojis\" name=\"Z\">"),
                new EmoteDefinition(":E_:", "<sprite=\"moremojis\" name=\"E\">"),
                new EmoteDefinition(":E2_:", "<sprite=\"moremojis\" name=\"E2\">"),
                new EmoteDefinition(":P_:", "<sprite=\"moremojis\" name=\"P\">"),
                new EmoteDefinition(":K_:", "<sprite=\"moremojis\" name=\"K\">"),
                new EmoteDefinition(":I_:", "<sprite=\"moremojis\" name=\"I\">"),
                new EmoteDefinition(":S_:", "<sprite=\"moremojis\" name=\"S\">"),
                new EmoteDefinition(":T_:", "<sprite=\"moremojis\" name=\"T\">"),
                new EmoteDefinition(":wisdom:", "<sprite=\"Zeepkist2\" name=\"Wisdom\">"),
                new EmoteDefinition(":ohno:", "<sprite=\"Zeepkist2\" name=\"OhNo\">")
            };
        }
    }
}
