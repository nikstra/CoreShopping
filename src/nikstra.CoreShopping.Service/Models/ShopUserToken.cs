using System;
using System.Collections.Generic;
using System.Text;

namespace nikstra.CoreShopping.Service.Models
{
    public class ShopUserToken
    {
        //public virtual string UserId { get; set; }
        public virtual string UserId { get; set; }
        public virtual string LoginProvider { get; set; }
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
    }
}
