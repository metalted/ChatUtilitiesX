namespace ChatUtilities.Data
{
    public class EmoteDefinition
    {
        public string Code { get; private set; }
        public string SpriteTag { get; private set; }

        public EmoteDefinition(string code, string spriteTag)
        {
            Code = code ?? string.Empty;
            SpriteTag = spriteTag ?? string.Empty;
        }
    }
}
