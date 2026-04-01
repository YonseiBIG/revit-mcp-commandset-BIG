using Newtonsoft.Json;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Models.Common
{
    public class WallTypeCreationInfo
    {
        [JsonProperty("wallTypeName")]
        public string WallTypeName { get; set; }

        [JsonProperty("layers")]
        public List<WallLayerInfo> Layers { get; set; }
    }

    public class WallLayerInfo
    {
        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("thickness")]
        public double Thickness { get; set; }

        [JsonProperty("function")]
        public string Function { get; set; }
    }
}
