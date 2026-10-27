namespace RtsServer.App.NetWorkDto
{
    public class NAirUnitReturnToPadRequest
    {
        public int[] UnitIds { get; set; } = System.Array.Empty<int>();

        public NAirUnitReturnToPadRequest() { }

        public NAirUnitReturnToPadRequest(int[] unitIds)
        {
            UnitIds = unitIds ?? System.Array.Empty<int>();
        }
    }
}
