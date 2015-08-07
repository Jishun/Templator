namespace Templator
{
    public class TemplatorSyntaxEventArgs
    {
        public bool HasError { get; set; }
        public int Position { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string TokenText { get; set; }
        public string TokenName { get; set; }
    }
}