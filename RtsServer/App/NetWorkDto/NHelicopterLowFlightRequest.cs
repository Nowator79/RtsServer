namespace RtsServer.App.NetWorkDto
{
    public class NHelicopterLowFlightRequest
    {
        public int UnitId { get; set; }

        public NHelicopterLowFlightRequest() { }

        public NHelicopterLowFlightRequest(int unitId)
        {
            UnitId = unitId;
        }
    }
}
