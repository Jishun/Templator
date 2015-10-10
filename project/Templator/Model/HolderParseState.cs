namespace Templator
{
    public class HolderParseState
    {
        public bool Begin;
        public bool Category;
        public bool Name;
        public bool KeywordsBegin;
        public bool KeywordParam;
        public bool KeywordParamBegin;
        public bool KeywordsEnd;
        public bool End;
        public bool Error;

        public override int GetHashCode()
        {
            if (Error)
            {
                return int.MinValue;
            }
            if (End)
            {
                return int.MaxValue;
            }
            var i = 0;
            return (Begin ? 1 : 0 )
                | ((Category || Name )? 1 : 0) << ++i
                | (Name ? 1 : 0) << ++i
                | (KeywordsBegin ? 1 : 0) << ++i
                | (KeywordParam ? 1 : 0) << ++i
                | (KeywordParamBegin ? 1 : 0) << ++i
                | (KeywordsEnd ? 1 : 0) << ++i;
        }

        public override bool Equals(object obj)
        {
            return obj is HolderParseState && ((HolderParseState) obj).GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return GetHashCode().ToString();
        }
    }
}
