namespace API.Entities
{
    public class Connection
    {
        public Connection()
        {
            
        }
        public Connection(string connectionId, string userName)
        {
            this.ConnectionId = connectionId;
            this.UserName = userName;

        }
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
    }
}