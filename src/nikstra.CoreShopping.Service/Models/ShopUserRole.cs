namespace nikstra.CoreShopping.Service.Models
{
    public class ShopUserRole
    {
        public string UserId { get; set; }
        public ShopUser ShopUser { get; set; }
        public string RoleId { get; set; }
        public ShopRole ShopRole { get; set; }
    }
}