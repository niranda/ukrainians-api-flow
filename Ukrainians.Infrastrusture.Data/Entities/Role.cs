using Microsoft.AspNetCore.Identity;

namespace Ukrainians.Infrastrusture.Data.Entities
{
    public class Role : IdentityRole<Guid>
    {
        public Role(string name)
        {
            Name = name;
        }
    }
}
