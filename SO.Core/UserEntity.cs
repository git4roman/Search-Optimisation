namespace SO.Core;

public class UserEntity
{
    protected UserEntity()
    {
        
    }
    public int Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string College { get; set; }
    public string University { get; set; }
    public string TechStack { get; set; }
    public string Program { get; set; }
    public string Address { get; set; }
}