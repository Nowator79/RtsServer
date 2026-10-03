using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public struct NMissile
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public Vector2Float Position { get; set; }
        /// <summary>Высота над картой.</summary>
        public float Height { get; set; }
        /// <summary>Рысканье (yaw) для ориентации модели.</summary>
        public float RotationYaw { get; set; }
        /// <summary>Тангаж (pitch) по вектору скорости.</summary>
        public float RotationPitch { get; set; }

        public NMissile(int id, string code, Vector2Float position, float height, float rotationYaw, float rotationPitch)
        {
            Id = id;
            Code = code;
            Position = position;
            Height = height;
            RotationYaw = rotationYaw;
            RotationPitch = rotationPitch;
        }
    }
}
