using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public enum MySqlValueGenerationStrategy
    {
        None = 0,
        IdentityColumn = 1,
        ComputedColumn = 2
    }
}
