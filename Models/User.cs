namespace PCBuilder.Models
{
    public class User
    {
        public int Id { get; set; } //primary key
        public string Username { get; set; } = "";

        //вече няма да пазим чиста парола
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; }
    }
}
