using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DLIOutletController.Models
{
    public class SwitchInfo
    {
        public string Name { get; set; }

        public OutletInfo[] Outlets { get; set; }
    }
}
