namespace Kodenames_DotNET.Models
{
    public enum Roles
    {
        PLAYER,
        SPYMASTER
    }
    public class Player
    {
        public string ConnectionId { get; set; }
        public string Nickname { get; set; }
        public Roles Role { get; set; }
    }
}