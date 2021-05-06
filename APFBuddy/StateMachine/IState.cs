using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APFBuddy
{
    public interface IState
    {
        void Tick();
        void OnStateEnter();
        void OnStateExit();
    }
}
