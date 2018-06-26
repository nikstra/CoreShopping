using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace nikstra.CoreShopping.Service.Models
{
    public class ShopUserClaim
    {
        public virtual int Id { get; set; }
        public virtual string ClaimType { get; set; }
        public virtual string ClaimValue { get; set; }
        public virtual string UserId { get; set; }
        //public string UserId { get; set; }

        public virtual Claim ToClaim()
        {
            return new Claim(ClaimType, ClaimValue);
        }

        public virtual void InitializeFromClaim(Claim claim)
        {
            ClaimType = claim?.Type;
            ClaimValue = claim?.Value;
        }
    }
}
