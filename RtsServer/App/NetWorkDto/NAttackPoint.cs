namespace RtsServer.App.NetWorkDto
{
    public struct NAttackPoint
    {
        public double Rotation { get; set; }
        public int ShotSerial { get; set; }
        /// <summary>Куда целился последний выстрел (мир XY) — для клиентского трассера.</summary>
        public float AimX { get; set; }
        public float AimY { get; set; }

        public NAttackPoint(double Rotation, int ShotSerial = 0, float AimX = 0f, float AimY = 0f)
        {
            this.Rotation = Rotation;
            this.ShotSerial = ShotSerial;
            this.AimX = AimX;
            this.AimY = AimY;
        }
    }
}
