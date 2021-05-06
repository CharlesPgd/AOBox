using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace InfBuddy
{
    public static class Extensions
    {
        public static void AddRandomness(this ref Vector3 pos, int entropy)
        {
            pos.X += Utils.Next(-entropy, entropy);
            pos.Z += Utils.Next(-entropy, entropy);
        }
    }
}
