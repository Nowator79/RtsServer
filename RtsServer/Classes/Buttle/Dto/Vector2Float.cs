namespace RtsServer.Classes.Buttle.Dto
{
    public struct Vector2Float
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2Float()
        {
            X = 0;
            Y = 0;
        }

        public Vector2Float(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
   
}
