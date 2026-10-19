namespace Asv.Common
{
    public abstract class TextTableBorder
    {
        protected abstract string Source { get; }

        public char TopLeft => Source[0];
        public char TopCenter => Source[1];
        public char TopIntersection => Source[2];
        public char TopRight => Source[3];

        public char Middle1Left => Source[4];
        public char Middle1Center => Source[5];
        public char Middle1Intersection => Source[6];
        public char Middle1Right => Source[7];

        public char Middle2Left => Source[8];
        public char Middle2Center => Source[9];
        public char Middle2Intersection => Source[10];
        public char Middle2Right => Source[11];

        public char BottomLeft => Source[12];
        public char BottomCenter => Source[13];
        public char BottomIntersection => Source[14];
        public char BottomRight => Source[15];
    }

    public class EmptyTextTableBorder : TextTableBorder
    {
        protected override string Source => "    " + "    " + "    " + "    ";
    }

    public class DoubleTextTableBorder : TextTableBorder
    {
        protected override string Source => "╔═╦╗" + "║ ║║" + "╠═╬╣" + "╚═╩╝";
    }

    public class SingleTextTableBorder : TextTableBorder
    {
        protected override string Source => "┌─┬┐" + "│ ││" + "├─┼┤" + "└─┴┘";
    }

    public class AsciiTableBorderSingle : TextTableBorder
    {
        protected override string Source => "+-++" + "| ||" + "+-++" + "+-++";
    }
}
