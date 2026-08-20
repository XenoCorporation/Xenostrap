using System;
using System.Collections.Generic;

namespace Xenostrap.Integrations
{
    public sealed class Server
    {
        public string Id { get; set; } = "";
        public int MaxPlayers { get; set; }
        public int Playing { get; set; }
        public double Fps { get; set; }
        public int Ping { get; set; }
    }

    public sealed class ServerResponse
    {
        public string? NextPageCursor { get; set; }
        public List<Server> Data { get; set; } = new();
    }
}
