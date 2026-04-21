using System;

namespace NETCode.Entities
{
    public class DataLogFromPLC
    {
        public string PalletID { get; set; }
        public string LocalTimeStamp { get; set; }
        public string StopID { get; set; }
        public string BuildResult { get; set; }
        public string DefectStationID { get; set; }

        public string DefectReason { get; set; }
        public string OperatorID { get; set; }
        public string PartModel { get; set; }
        public string PalletDestination { get; set; }
    }
}
