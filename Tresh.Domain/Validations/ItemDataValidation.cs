using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tresh.Domain.Models;

namespace Tresh.Domain.Validations
{
    public static class ItemDataValidation
    {
        public static bool Check(ItemData item)
        {
            if (item == null || item.Title == null || item.Description == null)
                return false;

            return true;
        }
    }
}
