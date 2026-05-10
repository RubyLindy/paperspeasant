namespace PP
{
    public enum SealType   { Valid, WrongKingdom, Expired, Forged }
    public enum ActorType  { Farmer, Merchant, ShadyGuy, Villager, Stranger, Beggar }
    public enum Action     { Accept, Deny, CallGuard }
    public enum Correct    { Accept, Deny, CallGuard }

    [System.Serializable]
    public class Document
    {
        public string title      = "Letter of Passage";
        public string issuedBy;
        public string bearer;
        public string purpose;
        public string validUntil;
        public SealType seal;

        public bool IsValid() => seal == SealType.Valid;

        public string SealLabel()
        {
            switch (seal)
            {
                case SealType.Valid:        return "Kingdom of Emmeloord  [VALID]";
                case SealType.WrongKingdom: return "Princedom of Vorn  [INVALID]";
                case SealType.Expired:      return "Kingdom of Emmeloord  [EXPIRED]";
                case SealType.Forged:       return "Kingdom of Emmeloord  [FORGED]";
                default: return "Unknown";
            }
        }
    }

    [System.Serializable]
    public class Visitor
    {
        public string    name;
        public ActorType type;
        public Document  doc;
        public Correct   correct;
        public string    greeting;
        public string    arrestLine;
    }

    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string text;
        public DialogueLine(string s, string t) { speaker = s; text = t; }
    }
}
