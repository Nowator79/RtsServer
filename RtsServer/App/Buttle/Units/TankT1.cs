﻿using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.Buttle.Units
{
    public class TankT1 : Unit
    {
        public TankT1(Vector2Float position) : base("TankT1", new(2000), position)
        {
            Speed = 7;
            RotationSpeed = 10;
        }
        public TankT1(Vector2Int position) : base("TankT1", new(2000), position)
        {
            Speed = 7;
            RotationSpeed = 10;
        }
    }
}
