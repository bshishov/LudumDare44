namespace Utils.Debugger
{
    public class DrawingContext
    {
        public float Y = 0f;
        public int Index = 0;
        public bool CollapseRequested = false;
        public bool ActionRequested = false;
        public int Depth = 0;
        public int CursorIndex = 0;
        public Style Style = new Style();
    }
}