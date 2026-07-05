using ChatUtilities.Data;
using System.Collections.Generic;
using ZeepSDK.ChatCommands;

namespace ChatUtilities
{
    public class ZeepSdkChatCommandReader
    {
        public static List<ChatCommandDefinition> CreateCommandsFromZeepSdk()
        {
            List<ChatCommandDefinition> result = new List<ChatCommandDefinition>();

            foreach (ILocalChatCommand command in ChatCommandRegistry.GetPrimaryLocalChatCommands())
            {
                result.Add(new ChatCommandDefinition(
                    command.Prefix + command.Command,
                    command.Description));

                foreach (ILocalChatCommandAlias alias in ChatCommandRegistry.GetAliasesFor(command))
                {
                    result.Add(new ChatCommandDefinition(
                        alias.Prefix + alias.Command,
                        alias.Description));
                }
            }

            return result;
        }
    }
}