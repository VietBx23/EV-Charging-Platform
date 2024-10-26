﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusEV.OCPP.Database
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string FirtName { get; set; }
        public string LastName { get; set; }
        public int Gender { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string TagId { get; set; }
        public int RoleCustomerID { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Images { get; set; }
        public DateTime CreateDate { get; set; }
        public virtual RoleCustomer RoleCustomer { get; set; }
    }
}