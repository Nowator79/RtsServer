namespace RtsServer.App.NetWorkDto
{
    public struct NAttackPoint
    {
        public double Rotation { get; set; }
        public int ShotSerial { get; set; }

        public NAttackPoint(double Rotation, int ShotSerial = 0)
        {
            this.Rotation = Rotation;
            this.ShotSerial = ShotSerial;
        }
    }
}
