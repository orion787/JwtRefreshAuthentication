using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tresh.Domain.Models;

public interface IItemDataDtoValidation
{
    public bool Check(ItemData item);
}
